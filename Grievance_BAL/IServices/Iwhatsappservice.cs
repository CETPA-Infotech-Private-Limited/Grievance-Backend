using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grievance_BAL.IServices
{
   public interface Iwhatsappservice
    {
        Task<bool> SendResolutionWhatsAppAsync(string userName, string grievanceId, string round, string acceptLink, string rejectLink, string mobileNumber);
    }
}
