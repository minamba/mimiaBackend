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
            CreateMap<Eleve, EleveViewModel>();

            // ParentId volontairement ignoré : il est imposé par le builder
            // depuis le JWT, jamais recopié depuis la requête.
            CreateMap<EleveRequest, Eleve>()
                .ForMember(dest => dest.ParentId, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DerniereActivite, opt => opt.Ignore())
                .ForMember(dest => dest.NiveauCode, opt => opt.Ignore())
                .ForMember(dest => dest.NiveauLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.NiveauCycle, opt => opt.Ignore());

            CreateMap<NiveauScolaire, NiveauScolaireViewModel>();
            CreateMap<Matiere, MatiereViewModel>();

            CreateMap<Conversation, ConversationViewModel>();
            CreateMap<Message, MessageViewModel>();
        }
    }
}
