using AutoMapper;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.ViewModels;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // LES CHOIX DE LA FAMILLE, SÉPARÉS POUR L'ÉCRAN : les langues d'un
            // côté, les spécialités de l'autre — voir `VoiesScolaires.LanguesAuChoix`.
            CreateMap<Eleve, EleveViewModel>()
                .ForMember(v => v.Specialites, o => o.MapFrom(e => VoiesScolaires.SansLangues(VoiesScolaires.SansChoixPro(e.Specialites))))
                .ForMember(v => v.Langues, o => o.MapFrom(e => VoiesScolaires.LanguesDe(e.Specialites)))
                // La filière pro voyage dans la même liste ; l'écran la reçoit à part.
                .ForMember(v => v.FamilleMetiers, o => o.MapFrom(e => VoiesScolaires.FiliereProDe(e.Specialites).Famille))
                .ForMember(v => v.SpecialitePro, o => o.MapFrom(e => VoiesScolaires.FiliereProDe(e.Specialites).Specialite))
                .ForMember(v => v.SpecialiteProLibre, o => o.MapFrom(e => VoiesScolaires.FiliereProDe(e.Specialites).Libre));

            // ParentId volontairement ignoré : il est imposé par le builder
            // depuis le JWT, jamais recopié depuis la requête.
            CreateMap<EleveRequest, Eleve>()
                .ForMember(dest => dest.ParentId, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DerniereActivite, opt => opt.Ignore())
                .ForMember(dest => dest.NiveauCode, opt => opt.Ignore())
                .ForMember(dest => dest.NiveauLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.NiveauCycle, opt => opt.Ignore())
                .ForMember(dest => dest.AcademieLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.Zone, opt => opt.Ignore())
                // L'établissement a sa propre route : le formulaire ne le porte pas.
                .ForMember(dest => dest.EtablissementUai, opt => opt.Ignore())
                .ForMember(dest => dest.EtablissementLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.EtablissementCommune, opt => opt.Ignore())
                .ForMember(dest => dest.EtablissementLatitude, opt => opt.Ignore())
                .ForMember(dest => dest.EtablissementLongitude, opt => opt.Ignore())
                .ForMember(dest => dest.BesoinsAccordLe, opt => opt.Ignore());

            CreateMap<NiveauScolaire, NiveauScolaireViewModel>();
            CreateMap<Matiere, MatiereViewModel>();
            CreateMap<Academie, AcademieViewModel>();

            CreateMap<Conversation, ConversationViewModel>();
            CreateMap<Message, MessageViewModel>();
        }
    }
}
