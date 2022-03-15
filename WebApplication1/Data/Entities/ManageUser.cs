﻿using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Data.Entities
{
    public class ManageUser:IdentityUser
    {
       
        public string DisPlayName { get; set; }

        public DateTime BirthDay { get; set; }


    }
}
