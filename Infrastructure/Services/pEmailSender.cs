using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Infrastructure.Services
{
    public class pEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<pEmailSender> _logger;
        public pEmailSender(IConfiguration config, ILogger<pEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            
            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
               
                await smtp.ConnectAsync(emailSettings["SmtpServer"],
                                       int.Parse(emailSettings["Port"]),
                                       SecureSocketOptions.StartTls);

                
                await smtp.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["Password"]);

               
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
                throw; // propagate for higher-level handling
            }
            finally
            {
               
                try
                {
                    await smtp.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while disconnecting SMTP client");
                }
            }
        }
    }
}
