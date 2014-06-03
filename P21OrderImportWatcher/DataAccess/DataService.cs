/** 
 * This file is part of the P21OrderImportWatcher project.
 * Copyright (c) 2014 Dai Nguyen
 * Author: Dai Nguyen
**/

using P21OrderImportWatcher.Models;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P21OrderImportWatcher.DataAccess
{
    public class DataService<T> : IDataService<T> where T : class
    {
        public DataContext Context { get; private set; }

        public DataService()
        {
            Context = new DataContext();
        }

        public DataService(DataContext context)
        {
            Context = context;
        }

        public async virtual Task<T> CreateAsync(T entity, CancellationToken token)
        {
            try
            {
                Context.Set<T>().Add(entity);

                if (await Context.SaveChangesAsync(token) > 0)
                    return entity;
            }
            catch { }
            return null;
        }

        public async virtual Task<T> UpdateAsync(T entity, CancellationToken token)
        {
            try
            {
                Context.Entry(entity).State = System.Data.Entity.EntityState.Modified;

                if (await Context.SaveChangesAsync(token) > 0)
                    return entity;
            }
            catch { }
            return null;
        }

        public async virtual Task<bool> DeleteAsync(T entity, CancellationToken token)
        {
            try
            {
                Context.Set<T>().Remove(entity);
                return await Context.SaveChangesAsync(token) > 0;
            }
            catch { }
            return false;
        }

        public async virtual Task<T> GetAsync(int id, CancellationToken token)
        {
            return await Context.Set<T>().FindAsync(id, token);
        }

        public IQueryable<T> All()
        {
            return Context.Set<T>();
        }

        public async Task<bool> SendDbMail(DbMailConfig config, string[] tos, string reply_to, string subject, string body)
        {
            string connStr = string.Format("server={0};database={1};user id={2};password={3}", config.Server, config.Db, config.User, config.Pass);
            SqlConnectionStringBuilder connStrBuilder = new SqlConnectionStringBuilder(connStr);
            connStrBuilder.AsynchronousProcessing = true;
            SqlConnection conn = new SqlConnection(connStrBuilder.ToString());

            using (SqlCommand cmd = new SqlCommand(config.StoredProcedure, conn))
            {

                StringBuilder builder = new StringBuilder();
                foreach (string to in tos)
                {
                    builder.Append(to + ";");
                }

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@profile_name", config.Profile);
                cmd.Parameters.AddWithValue("@recipients", builder.ToString());
                cmd.Parameters.AddWithValue("@copy_recipients", "");
                cmd.Parameters.AddWithValue("@blind_copy_recipients", "");
                cmd.Parameters.AddWithValue("@subject", subject);
                cmd.Parameters.AddWithValue("@body", body);
                cmd.Parameters.AddWithValue("@body_format", "HTML");

                if (!string.IsNullOrEmpty(reply_to))
                    cmd.Parameters.AddWithValue("@reply_to", reply_to);

                try
                {
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
                catch { }
                finally
                {
                    conn.Close();
                }
                return false;
            }
        }

        public async Task<bool> SendSmtpEmailAsync(SmtpMailConfig smtp, string[] tos, string subject, string body)
        {
            try
            {
                using (MailMessage msg = new MailMessage())
                {
                    msg.From = new MailAddress(smtp.EmailFrom);

                    foreach (string t in tos)
                    {
                        msg.To.Add(t);
                    }

                    msg.Subject = subject;
                    msg.Body = body;
                    msg.IsBodyHtml = true;

                    using (SmtpClient client = new SmtpClient(smtp.SmtpHost, smtp.SmtpPort))
                    {
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.Credentials = new NetworkCredential(smtp.SmtpUser, smtp.SmtpPass);
                        client.EnableSsl = smtp.EnableSSL;
                        await client.SendMailAsync(msg);
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
