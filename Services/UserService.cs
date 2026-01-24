using JournalApplicaton.Common;
using JournalApplicaton.Data;
using JournalApplicaton.Entities;
using JournalApplicaton.Model;
using Microsoft.EntityFrameworkCore;
using System;

namespace JournalApplicaton.Services;

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
            Id = user.Id,
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

            //  Check duplicate username
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

            //  Map Entity → Display Model
            var displayModel = new UserDisplayModel
            {
                Id = user.Id,
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
            Id = user.Id,
            Name = user.FullName,
            Username = user.Username,
            Email = user.Email
        };

        return ServiceResult<UserDisplayModel>.SuccessResult(display);
    }
    public async Task<ServiceResult<List<UserDisplayModel>>> GetAllUsersAsync()
    {
        try
        {
            var users = await _context.Users.ToListAsync();

            //  Map entities to display models
            var displayModels = users.Select(MapToDisplayModel).ToList();

            return ServiceResult<List<UserDisplayModel>>.SuccessResult(displayModels);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<UserDisplayModel>>.FailureResult($"Error retrieving users: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UserDisplayModel>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult<UserDisplayModel>.FailureResult($"User with ID {id} not found");
            }

            // Map entity to display model
            var displayModel = MapToDisplayModel(user);

            return ServiceResult<UserDisplayModel>.SuccessResult(displayModel);
        }
        catch (Exception ex)
        {
            return ServiceResult<UserDisplayModel>.FailureResult($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UserDisplayModel>> UpdateUserAsync(int id, UserViewModel viewModel)
    {
        try
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return ServiceResult<UserDisplayModel>.FailureResult($"User with ID {id} not found");
            }

            // Check for duplicate email (excluding current user)
            var duplicateEmail = await _context.Users
                .AnyAsync(u => u.Email == viewModel.Email && u.Id != id);

            if (duplicateEmail)
            {
                return ServiceResult<UserDisplayModel>.FailureResult($"Email {viewModel.Email} is already in use");
            }

            // Map ViewModel to Entity
            existingUser.FullName = viewModel.Name;
            existingUser.Email = viewModel.Email;

            await _context.SaveChangesAsync();

            // ✅ Map entity to display model
            var displayModel = MapToDisplayModel(existingUser);

            return ServiceResult<UserDisplayModel>.SuccessResult(displayModel);
        }
        catch (Exception ex)
        {
            return ServiceResult<UserDisplayModel>.FailureResult($"Error updating user: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteUserAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult<bool>.FailureResult($"User with ID {id} not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting user: {ex.Message}");
        }
    }
}