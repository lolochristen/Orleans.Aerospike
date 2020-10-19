using System;
using System.Collections.Generic;

namespace OrleansHotelBooking.Interfaces
{
    public class Guest
    {
        public string Name { get; set; }
        public IList<Guid> Bookings { get; set; }
        public string Email { get; set; }
    }


}
