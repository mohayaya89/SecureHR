using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public sealed class DepartmentListItemDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int EmployeeCount { get; init; }
    }
}
