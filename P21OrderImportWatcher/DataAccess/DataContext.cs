/** 
 * This file is part of the P21OrderImportWatcher project.
 * Copyright (c) 2014 Dai Nguyen
 * Author: Dai Nguyen
**/

using P21OrderImportWatcher.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace P21OrderImportWatcher.DataAccess
{
    public class DataContext : DbContext
    {
        public DataContext()
        {
            Database.SetInitializer<DataContext>(null);
        }

        public DbSet<FileImport> FileImports { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
