using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IkuaiApi
{
    public class Common
    {
        public static CookieCollection CookieCollection { get; set; } =new CookieCollection();
        public static bool isCookieTimeout = false;
    }
}
