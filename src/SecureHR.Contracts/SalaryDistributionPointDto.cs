using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class SalaryDistributionPointDto
    {
        public string Bucket { get; set; } = string.Empty;         // e.g. "$40k-$60k"
        public int Count { get; set; }
    }
}
