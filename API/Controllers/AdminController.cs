using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _uow;
        public AdminController(UserManager<AppUser> userManager, IUnitOfWork uow)
        {
            _uow = uow;
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new 
                {
                    u.Id, 
                    Username = u.UserName,
                    Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery]string roles)
        {
            if(string.IsNullOrEmpty(roles)) return BadRequest("You must select at least one role");

            var selectedRoles = roles.Split(",").ToArray();

            var user = await _userManager.FindByNameAsync(username);

            if(user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if(!result.Succeeded) return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if(!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public async Task<ActionResult<IEnumerable<PhotoForApprovalDto>>> GetPhotosForModeration()
        {
            var unnaprovedPhotos = await _uow.PhotoRepository.GetUnnaprovedPhotos();
            return Ok(unnaprovedPhotos);
        }

        // [Authorize(Policy = "ModeratePhotoRole")]
        // [HttpGet("photos-for-approval")]
        // public async Task<ActionResult<IEnumerable<PhotoForApprovalDto>>> GetPhotosForApproval()
        // {
        //     var unnaprovedPhotos = await _uow.PhotoRepository.GetUnnaprovedPhotos();
        //     return Ok(unnaprovedPhotos);
        // }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approve-photo/{photoId}")]
        public async Task<ActionResult<PhotoForApprovalDto>> ApprovePhoto(int photoId)
        {
            var photo = await _uow.PhotoRepository.GetPhotoById(photoId);

            if(photo == null) return NotFound();

            if(photo.IsApproved) return BadRequest("This photo is already approved");

            // Check if user has main photo...
            photo.IsApproved = true;
            
            var user = await _uow.UserRepository.GetUserByPhotoId(photoId);
            
            if(user == null) return BadRequest("User does not exist");

            if(!user.Photos.Any(p => p.IsMain == true)) // ....if not, set to main
                photo.IsMain = true;
            
            if(await _uow.Complete()) return Ok(new PhotoForApprovalDto
                {
                    Id = photo.Id,
                    Url = photo.Url,
                    Username = user.UserName,
                    IsApproved = photo.IsApproved
                });

            return BadRequest("Problem approving the photo");
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("reject-photo/{photoId}")]
        public async Task<ActionResult<PhotoForApprovalDto>> RejectPhoto(int photoId)
        {
            var photo = await _uow.PhotoRepository.GetPhotoById(photoId);

            if(photo == null) return NotFound();

            if(!photo.IsApproved) return BadRequest("This photo has already been rejected");

            photo.IsApproved = false;

            var user = await _uow.UserRepository.GetUserByPhotoId(photoId);
            
            if(user == null) return BadRequest("User does not exist");
            
            // No unnaproved photo should be main
            if(photo.IsMain)
                photo.IsMain = false;

            if(await _uow.Complete()) return Ok(new PhotoForApprovalDto
                {
                    Id = photo.Id,
                    Url = photo.Url,
                    Username = user.UserName,
                    IsApproved = photo.IsApproved
                });

            return BadRequest("Problem rejecting the photo");
        }

    }
}