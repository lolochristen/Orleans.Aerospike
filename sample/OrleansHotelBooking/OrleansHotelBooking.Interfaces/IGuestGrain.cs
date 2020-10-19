using System;
using System.Threading.Tasks;
using Orleans;

namespace OrleansHotelBooking.Interfaces
{
    public interface IGuestGrain : IGrainWithStringKey
    {
        //[Transaction(TransactionOption.Join)]
        Task UpdateInfo(Guest guestInfo);

        //[Transaction(TransactionOption.Join)]
        Task AssignBooking(Guid bookingId);
    }


}
