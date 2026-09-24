using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Auth.Commands.GoogleRegister;

public class GoogleRegisterCommandHandler : IRequestHandler<GoogleRegisterCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public GoogleRegisterCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuthService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(GoogleRegisterCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await _googleAuthService.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        var emailNormalized = googleUser.Email.Trim().ToLowerInvariant();

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.GoogleId ||
                                      u.Email.ToLower() == emailNormalized ||
                                      (u.GoogleEmail != null && u.GoogleEmail.ToLower() == emailNormalized),
                                 cancellationToken);

        User user;
        if (existingUser != null)
        {
            user = existingUser;
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
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = googleUser.FirstName,
                LastName = string.IsNullOrWhiteSpace(googleUser.LastName) ? "." : googleUser.LastName,
                Email = emailNormalized,
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? "Not Provided" : request.Phone.Trim(),
                PasswordHash = "OAUTH_GOOGLE_ACCOUNT",
                Department = string.IsNullOrWhiteSpace(request.Department) ? "Genel" : request.Department.Trim(),
                RoleType = RoleType.Employee,
                AvatarUrl = googleUser.PictureUrl,
                GoogleId = googleUser.GoogleId,
                GoogleEmail = googleUser.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

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

        return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Google ile kayıt başarıyla tamamlandı.");
    }
}
