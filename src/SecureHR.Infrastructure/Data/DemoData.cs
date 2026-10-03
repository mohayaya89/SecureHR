namespace SecureHR.Infrastructure.Data
{
    /// <summary>
    /// Seed data, each added only when the database has none yet:
    ///   - Departments: every environment (a starting set; rename or deactivate them in the app).
    ///   - Employees: Development only, so a fresh database (e.g. after deleting securehr.db)
    ///     always has the same demo data. Other environments start without employees.
    /// All people, emails and job titles are invented; no sensitive personal data is stored.
    /// To add demo data, add it here (it is never copied from the database automatically).
    /// </summary>
    internal static class DemoData
    {
        public sealed record DemoEmployee(
            string FirstName,
            string LastName,
            string Department,
            DateOnly HireDate,
            decimal AnnualSalary,
            string JobTitle)
        {
            public string Email => $"{FirstName}.{LastName}@securehr.com".ToLowerInvariant();
        }

        public sealed record DemoDepartment(string Name);

        internal static IReadOnlyList<DemoDepartment> Departments => departments;
        internal static IReadOnlyList<DemoEmployee> Employees => employees;

        // Order matters: departments get Ids in this order (Engineering = 1, Sales = 2, ...)
        private static readonly IReadOnlyList<DemoDepartment> departments =
        [
            new("Engineering"),
            new("Sales"),
            new("Human Resources"),
            new("Finance"),
            new("Training and Development"),
            new("Corporate Communications"),
            new("Risk Management"),
            new("Business Development"),
            new("Security"),
            new("Marketing"),
            new("General Management"),
            new("Operations"),
            new("Product Management"),
            new("Information Technology"),
            new("Customer Service"),
            new("Legal"),
            new("Compliance & Regulatory Affairs"),
            new("Quality Assurance"),
            new("Research and Development"),
            new("Data Analytics"),
            new("Procurement"),
            new("Facilities & Workplace Management")
        ];

        // Each Department must be one of the names above
        private static readonly IReadOnlyList<DemoEmployee> employees =
        [
            new("John", "Smith", "Engineering", new DateOnly(2020, 1, 15), 95000m, "Senior Software Engineer"),
            new("Jane", "Doe", "Sales", new DateOnly(2021, 3, 22), 72000m, "Account Executive"),
            new("Liam", "Johnson", "Customer Service", new DateOnly(2021, 10, 20), 77000m, "Customer Service Representative"),
            new("Olivia", "Rodriguez", "Corporate Communications", new DateOnly(2022, 3, 23), 63500m, "Communications Specialist"),
            new("Noah", "Thomas", "Operations", new DateOnly(2026, 9, 3), 72000m, "Operations Coordinator"),
            new("Emma", "Thompson", "Information Technology", new DateOnly(2018, 2, 24), 72000m, "IT Support Specialist"),
            new("Oliver", "Robinson", "Operations", new DateOnly(2016, 12, 27), 50000m, "Operations Manager"),
            new("Ava", "Miller", "Data Analytics", new DateOnly(2022, 6, 1), 48000m, "Data Analyst"),
            new("Elijah", "Wilson", "Sales", new DateOnly(2018, 7, 28), 64500m, "Sales Representative"),
            new("Sophia", "Lee", "Security", new DateOnly(2020, 12, 10), 70000m, "Security Analyst"),
            new("Lucas", "Ramirez", "Procurement", new DateOnly(2024, 2, 4), 58000m, "Procurement Specialist"),
            new("Isabella", "Jones", "Business Development", new DateOnly(2017, 9, 17), 60500m, "Business Development Manager"),
            new("Mason", "Lopez", "Quality Assurance", new DateOnly(2023, 7, 21), 51500m, "QA Engineer"),
            new("Mia", "Jackson", "Product Management", new DateOnly(2025, 5, 7), 70000m, "Product Manager"),
            new("Ethan", "Sanchez", "Engineering", new DateOnly(2025, 3, 17), 61000m, "Software Engineer"),
            new("Amelia", "Williams", "Information Technology", new DateOnly(2018, 7, 9), 53500m, "Systems Administrator"),
            new("Logan", "Martinez", "Facilities & Workplace Management", new DateOnly(2021, 11, 19), 72000m, "Facilities Coordinator"),
            new("Harper", "Taylor", "Operations", new DateOnly(2019, 5, 5), 70500m, "Logistics Specialist"),
            new("James", "White", "Compliance & Regulatory Affairs", new DateOnly(2017, 9, 28), 74000m, "Compliance Officer"),
            new("Evelyn", "Walker", "Information Technology", new DateOnly(2024, 12, 12), 51500m, "Network Engineer"),
            new("Aiden", "Davis", "Product Management", new DateOnly(2023, 2, 25), 63000m, "Product Owner"),
            new("Abigail", "Anderson", "Product Management", new DateOnly(2022, 12, 26), 72000m, "Associate Product Manager"),
            new("Carter", "Perez", "Training and Development", new DateOnly(2017, 9, 26), 69500m, "Training Specialist"),
            new("Emily", "Lewis", "Corporate Communications", new DateOnly(2018, 9, 28), 63000m, "Content Writer"),
            new("Sebastian", "Garcia", "Business Development", new DateOnly(2022, 6, 10), 54500m, "Partnerships Associate"),
            new("Ella", "Gonzalez", "Business Development", new DateOnly(2016, 3, 13), 53500m, "Business Analyst"),
            new("Jack", "Martin", "Research and Development", new DateOnly(2026, 1, 23), 71000m, "Research Scientist"),
            new("Avery", "Clark", "Human Resources", new DateOnly(2023, 2, 8), 58000m, "HR Generalist"),
            new("Owen", "Brown", "Corporate Communications", new DateOnly(2026, 3, 2), 45500m, "Communications Manager"),
            new("Scarlett", "Hernandez", "Finance", new DateOnly(2021, 10, 19), 62000m, "Financial Analyst"),
            new("Henry", "Moore", "Training and Development", new DateOnly(2025, 4, 16), 59500m, "Learning and Development Manager"),
            new("Grace", "Harris", "Compliance & Regulatory Affairs", new DateOnly(2016, 4, 2), 74000m, "Regulatory Analyst"),
        ];

        

    }
}
