using System;
using System.Collections.Generic;

namespace OrleansHotelBooking.Interfaces
{
    public class Hotel
    {
        public string HotelName { get; set; }
        public IDictionary<DateTime, int> AvailableRooms { get; set; }
        public IList<Guid> Bookings { get; set; }
    }
}
