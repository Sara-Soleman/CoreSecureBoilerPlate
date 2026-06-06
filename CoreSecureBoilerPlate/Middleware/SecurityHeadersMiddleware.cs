namespace CoreSecureBoilerPlate.Middleware
{
    public class SecurityHeadersMiddleware(RequestDelegate next)
    {
        //CORS - Cross-Origin Resource Sharing
        //This is a critical security feature that controls how resources on your server can be accessed by web pages from different origins.
        //Properly configuring CORS helps prevent unauthorized access and data leaks.
        //Security Headers
        public async Task InvokeAsync(HttpContext context)
        {
            //Prevent the browser from guessing the MIME type (Defends against MIME-sniffing attacks)
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            //Anti-clickjacking protection: Establishes that this site cannot be embedded framed inside other websites
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            //Enables the Cross-Site Scripting (XSS) filter built into modern web browsers
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            //Referrer Policy: Controls how much referrer information the browser includes with navigations
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            //Strict-Transport-Security (HSTS): Forces HTTPS communication exclusively (Inject only in non-development)
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            await next(context);
        }
    }
}
