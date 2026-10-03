using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class HeadcountByDepartmentDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
