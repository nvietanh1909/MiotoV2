using Mioto.Models.ZaloPay;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Mioto.DAL
{
    public class ZaloPayDemoContext : DbContext
    {
        public ZaloPayDemoContext() : base("ZaloPayDemoSqlServer")
        {
            Configuration.ValidateOnSaveEnabled = false;
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Refund> Refunds { get; set; }
    }
}