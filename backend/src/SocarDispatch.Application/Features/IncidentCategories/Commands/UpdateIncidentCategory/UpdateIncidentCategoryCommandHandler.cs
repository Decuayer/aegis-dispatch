using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.IncidentCategories.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.IncidentCategories.Commands.UpdateIncidentCategory;

public class UpdateIncidentCategoryCommandHandler : IRequestHandler<UpdateIncidentCategoryCommand, ApiResponse<IncidentCategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateIncidentCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<IncidentCategoryDto>> Handle(UpdateIncidentCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.IncidentCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null)
        {
            throw new EntityNotFoundException("IncidentCategory", request.Id);
        }

        var codeExists = await _context.IncidentCategories
            .AnyAsync(c => c.Code == request.Code && c.Id != request.Id, cancellationToken);

        if (codeExists)
        {
            throw new DomainException($"Incident category code '{request.Code}' is already in use by another category.");
        }

        category.Code = request.Code;
        category.Name = request.Name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;

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

        return ApiResponse<IncidentCategoryDto>.SuccessResult(dto, "Incident category updated successfully.");
    }
}
