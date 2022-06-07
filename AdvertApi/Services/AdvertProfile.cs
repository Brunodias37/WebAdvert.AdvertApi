using AutoMapper;
namespace AdvertApi.Services
{
    public class AdvertProfile:Profile
    {
        
        public AdvertProfile()
        {
            //ira mapear de acordo com os nomes
            CreateMap<AdvertDbModel, AdvertDbModel>();
        }
    }
}
