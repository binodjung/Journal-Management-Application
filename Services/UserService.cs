using JournalApplication.Model;
using JournalApplication.Data;
using JournalApplication.Common;
using Microsoft.EntityFrameworkCore;
using JournalApplication.Entities;

namespace JournalApplication.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    // Private helper method to map Entity to DisplayModel
    private UserDisplayModel MapToDisplayModel(User user)
    {
        return new UserDisplayModel
        {
            UserId = user.UserId,
            Name = user.FullName,
            Email = user.Email,
        };
    }
    public async Task<ServiceResult<UserDisplayModel>> RegisterUserAsync(UserViewModel viewModel)
    {
        try
        {
            // Check duplicate email
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == viewModel.Email);

            if (emailExists)
            {
                return ServiceResult<UserDisplayModel>
                    .FailureResult("Email already registered");
            }

            // Check duplicate username
            bool usernameExists = await _context.Users
                .AnyAsync(u => u.Username == viewModel.Username);

            if (usernameExists)
            {
                return ServiceResult<UserDisplayModel>
                    .FailureResult("Username already taken");
            }

            // Hash password using BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(viewModel.Password);

            // Map ViewModel → Entity
            var user = new User
            {
                FullName = viewModel.Name,
                Username = viewModel.Username,
                Email = viewModel.Email,
                Password = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Map Entity → Display Model
            var displayModel = new UserDisplayModel
            {
                UserId = user.UserId,
                Name = user.FullName,
                Username = user.Username,
                Email = user.Email
            };

            return ServiceResult<UserDisplayModel>.SuccessResult(displayModel);
        }
        catch (Exception ex)
        {
            return ServiceResult<UserDisplayModel>
                .FailureResult($"Error adding user: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UserDisplayModel>> LoginUserAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return ServiceResult<UserDisplayModel>.FailureResult("Invalid username or password");

        bool verified = BCrypt.Net.BCrypt.Verify(password, user.Password);

        if (!verified)
            return ServiceResult<UserDisplayModel>.FailureResult("Invalid username of password");

        var display = new UserDisplayModel
        {
            UserId = user.UserId,
            Name = user.FullName,
            Username = user.Username,
            Email = user.Email
        };

        return ServiceResult<UserDisplayModel>.SuccessResult(display);
    }
}