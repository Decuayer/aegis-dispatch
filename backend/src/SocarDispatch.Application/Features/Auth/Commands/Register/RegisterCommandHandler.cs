using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();

        // 1. Check if the email or phone is already registered in the database
        var existingEmail = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);

        if (existingEmail)
        {
            throw new DomainException("A user registered with this email address already exists.");
        }

        var phoneTrimmed = request.Phone.Trim();
        var existingPhone = await _context.Users
            .AnyAsync(u => u.Phone == phoneTrimmed, cancellationToken);

        if (existingPhone)
        {
            throw new DomainException("A user with this phone number already exists.");
        }

        // 2. Hashing the password with BCrypt
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 3. Creating a new User entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = emailNormalized,
            Phone = request.Phone.Trim(),
            PasswordHash = passwordHash,
            Department = request.Department.Trim(),
            RoleType = request.RoleType,
            SubRole = request.SubRole?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Generate and return a JWT
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        var authResponse = new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Department = user.Department,
                RoleType = user.RoleType,
                SubRole = user.SubRole,
                AvatarUrl = user.AvatarUrl
            }
        };

        return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "User registration was successfully created.");
    }
}