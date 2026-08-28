
using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.IncidentCategories.DTOs;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.IncidentCategories.Commands.CreateIncidentCategory;

public class CreateIncidentCategoryCommandHandler : IRequestHandler<CreateIncidentCategoryCommand, ApiResponse<IncidentCategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateIncidentCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<IncidentCategoryDto>> Handle(CreateIncidentCategoryCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.IncidentCategories
            .AnyAsync(c => c.Code == request.Code, cancellationToken);

        if (exists)
        {
            throw new DomainException($"Incident category code '{request.Code}' already exists.");
        }

        var category = new IncidentCategory
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.IncidentCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new IncidentCategoryDto
        {
            Id = category.Id,
            Code = category.Code,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return ApiResponse<IncidentCategoryDto>.SuccessResult(dto, "Incident category created successfully.");
    }
}
