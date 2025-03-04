namespace Grievance_Model.DTOs.Dashboard
{
    public class DashboardModel
    {
        public int TotalGrievance { get; set; }
        public int Pending { get; set; } //Created
        public int InProgress { get; set; } // InProgress
        public int Resolved { get; set; } //Resolved

        public int Unresolved { get; set; } // InProgress + Created
        public int Over7Days { get; set; }
        public int Over14Days { get; set; }
        public int ThisMonth { get; set; }
        public int Over30Days { get; set; }
        public List<MonthGrievance> MonthlyGrievances { get; set; } = new();
    }

    public class MonthGrievance
    {
        public string MonthName { get; set; } 
        public int MonthInt { get; set; } 
        public int TotalCount { get; set; } 
    }

}
