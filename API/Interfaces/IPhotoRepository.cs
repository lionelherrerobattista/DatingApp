using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IPhotoRepository
    {
        Task<IEnumerable<PhotoForApprovalDto>> GetUnnaprovedPhotos();
        Task<Photo> GetPhotoById(int id);
        void RemovePhoto(Photo photo);
    }
}