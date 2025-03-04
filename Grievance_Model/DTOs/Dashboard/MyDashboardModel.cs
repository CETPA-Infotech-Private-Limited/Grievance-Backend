namespace Grievance_Model.DTOs.Dashboard
{
    public class MyDashboardModel
    {
        public int TotalGrievance { get; set; }
        public int InProgress { get; set; } // InProgress + Created
        public int Resolved { get; set; } //Resolved
        public object RecentGrievances { get; set; }
        public List<MonthGrievance> MonthlyGrievances { get; set; } = new();
    }

}
