using Concentus;
using Concentus.Enums;
using Concentus.Oggfile;

namespace SchoolWebApp.Api.Services.Podcasts
{
    /// <summary>
    /// L'AUDIO D'UN ÉPISODE : ASSEMBLÉ, COMPRESSÉ, ET RELU POUR LES VIEUX NAVIGATEURS — 15/11/2026.
    ///
    /// LA VOIX ARRIVE EN PCM BRUT (16 bits, mono, 24 kHz), paragraphe par paragraphe. En WAV, cinq minutes pèsent 14 Mo :
    /// trop pour un téléphone en 4G. On les range en OGG OPUS, le format fait pour la voix, encodé en C# pur (Concentus :
    /// rien à installer dans le conteneur) — mesuré le 15/11/2026 : 17 Ko pour 5 s, soit environ 1 Mo pour 5 minutes.
    ///
    /// UN IPHONE SOUS iOS 18.4 OU PLUS LIT L'OGG OPUS ; un plus ancien non. Pour lui, l'écran demande le WAV, que l'on
    /// refait ici à la volée depuis l'Opus — plus lourd, mais il passe partout.
    /// </summary>
    public static class AudioPodcast
    {
        public const int Frequence = 24000;

        /// <summary>La fréquence où le décodeur de Concentus décode juste (voir `EnWav`).</summary>
        private const int FrequenceDecodage = 48000;

        /// <summary>32 kbit/s : la voix reste nette, une minute ≈ 240 Ko.</summary>
        private const int Debit = 32000;

        /// <summary>Un silence de tant de secondes, en PCM.</summary>
        public static byte[] Silence(double secondes) => new byte[(int)(Frequence * secondes) * 2];

        public static byte[] EncoderOpus(byte[] pcm)
        {
            var echantillons = new short[pcm.Length / 2];
            Buffer.BlockCopy(pcm, 0, echantillons, 0, echantillons.Length * 2);

            var encodeur = OpusCodecFactory.CreateEncoder(Frequence, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            encodeur.Bitrate = Debit;

            using var sortie = new MemoryStream();
            var ogg = new OpusOggWriteStream(encodeur, sortie);
            ogg.WriteSamples(echantillons, 0, echantillons.Length);
            ogg.Finish();
            return sortie.ToArray();
        }

        /// <summary>La durée d'un PCM 16 bits mono à 24 kHz, en secondes.</summary>
        public static int Duree(long octetsPcm) => (int)Math.Round(octetsPcm / 2d / Frequence);

        /// <summary>L'Opus relu en WAV 16 bits mono 24 kHz, pour un navigateur qui ne lit pas l'Opus.</summary>
        public static byte[] EnWav(byte[] opus)
        {
            // DÉCODÉ EN 48 kHz, PUIS RAMENÉ À 24 kHz — 15/11/2026, MP3 inaudible chez Camara. Le décodeur de Concentus
            // demandé en 24 kHz rend du BRUIT (mesuré sur un vrai épisode : -37 dB de moyenne et une corrélation de
            // 0,07 avec le son décodé par ffmpeg) ; en 48 kHz, il rend la bonne voix (-19 dB, comme ffmpeg). L'Opus
            // vocal ne monte pas au-delà de 12 kHz : la moyenne de deux échantillons voisins suffit à redescendre.
            var decodeur = OpusCodecFactory.CreateDecoder(FrequenceDecodage, 1);
            var lecteur = new OpusOggReadStream(decodeur, new MemoryStream(opus));

            using var pcm = new MemoryStream();
            while (lecteur.HasNextPacket)
            {
                var paquet = lecteur.DecodeNextPacket();
                if (paquet is null) continue;
                var octets = new byte[paquet.Length / 2 * 2];
                for (int i = 0, j = 0; i + 1 < paquet.Length; i += 2, j += 2)
                {
                    var v = (short)((paquet[i] + paquet[i + 1]) / 2);
                    octets[j] = (byte)v;
                    octets[j + 1] = (byte)(v >> 8);
                }

                pcm.Write(octets);
            }

            var donnees = pcm.ToArray();
            using var flux = new MemoryStream(44 + donnees.Length);
            using var w = new BinaryWriter(flux);
            w.Write("RIFF"u8);
            w.Write(36 + donnees.Length);
            w.Write("WAVE"u8);
            w.Write("fmt "u8);
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(Frequence);
            w.Write(Frequence * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write("data"u8);
            w.Write(donnees.Length);
            w.Write(donnees);
            return flux.ToArray();
        }
    }
}
