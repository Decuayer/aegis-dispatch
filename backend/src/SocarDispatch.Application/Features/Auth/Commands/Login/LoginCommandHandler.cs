using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();

        // 1. Finding the user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);

        if (user == null)
        {
            throw new DomainException("Incorrect email address or password.");
        }

        // 2. Verifying the password with BCrypt
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new DomainException("Incorrect email address or password.");
        }

        // 3. JWT generation and DTO mapping
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

        return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Login successful.");
    }
}