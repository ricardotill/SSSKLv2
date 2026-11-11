using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Export;

internal sealed class GetOrderCsvExportStatusHandler(OrderCsvExportJob exportJob)
    : IRequestHandler<GetOrderCsvExportStatusQuery, CsvExportJobDto?>
{
    public Task<CsvExportJobDto?> Handle(GetOrderCsvExportStatusQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(exportJob.GetStatus(request.JobId));
}
