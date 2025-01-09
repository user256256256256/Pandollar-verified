using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailSender : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string message)
    {
        // Log start of method
        Console.WriteLine("Starting to send email...");

        // Set up the email message
        var mailMessage = new MailMessage
        {
            From = new MailAddress("admin@lyfexafrica.com"),  // Your email address here
            Subject = subject,
            Body = message,
            IsBodyHtml = true,  // Set to true if the message is in HTML format
        };

        // Log email setup details
        Console.WriteLine($"Email configured. Subject: {subject}, Recipient: {email}");

        // Add the recipient email
        mailMessage.To.Add(email);
        Console.WriteLine($"Recipient {email} added to the email.");

        // Set up the SMTP client with your server details
        using (var client = new SmtpClient("lyfexafrica.com", 587)  // Replace with your SMTP server and port (e.g., 465)
        {
            Credentials = new NetworkCredential("admin@lyfexafrica.com", "Planchinobo256"),  // Replace with your SMTP credentials
            EnableSsl = true,  // Use SSL for secure communication
        })
        {
            try
            {
                Console.WriteLine("SMTP client configured. Sending email...");

                // Send the email asynchronously
                await client.SendMailAsync(mailMessage);
                Console.WriteLine("Email sent successfully.");
            }
            catch (TaskCanceledException ex)
            {
                // Log the timeout error
                Console.WriteLine($"Error while sending email: Task was canceled. Timeout might have occurred. Error: {ex.Message}");
                throw new Exception("Email sending timed out. Please try again.");
            }
            catch (Exception ex)
            {
                // Log any other errors during email sending
                Console.WriteLine($"Error while sending email: {ex.Message}");
                throw;  // Re-throw the exception to ensure it's not silently caught
            }
        }
    }
}
