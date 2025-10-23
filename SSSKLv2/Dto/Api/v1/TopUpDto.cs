using System;

namespace SSSKLv2.Dto.Api.v1;

public class TopUpDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public decimal Saldo { get; set; }
}

