using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public GoogleLoginCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuthService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Verifying the Google ID Token
        var googleUser = await _googleAuthService.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        var emailNormalized = googleUser.Email.Trim().ToLowerInvariant();

        // 2. Check if the user exists in the database by GoogleId or normalized Email/GoogleEmail.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.GoogleId ||
                                      u.Email.ToLower() == emailNormalized ||
                                      (u.GoogleEmail != null && u.GoogleEmail.ToLower() == emailNormalized),
                                 cancellationToken);

        // 3. Automatically save if the user is visiting for the first time
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = googleUser.FirstName,
                LastName = string.IsNullOrWhiteSpace(googleUser.LastName) ? "." : googleUser.LastName,
                Email = emailNormalized,
                Phone = "Not Provided",
                PasswordHash = "OAUTH_GOOGLE_ACCOUNT",
                Department = "Genel",
                RoleType = RoleType.Employee,
                AvatarUrl = googleUser.PictureUrl,
                GoogleId = googleUser.GoogleId,
                GoogleEmail = googleUser.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var isDirty = false;
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleUser.GoogleId;
                user.GoogleEmail = googleUser.Email;
                isDirty = true;
            }

            if (!string.IsNullOrEmpty(googleUser.PictureUrl) && string.IsNullOrEmpty(user.AvatarUrl))
            {
                user.AvatarUrl = googleUser.PictureUrl;
                isDirty = true;
            }

            if (isDirty)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // 4. Generating a JWT
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
                AvatarUrl = user.AvatarUrl,
                GoogleEmail = user.GoogleEmail
            }
        };

        return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Google ile giriş başarılı.");
    }
}
