using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.Asn1.Ocsp;

namespace sendotp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OTPController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly IMemoryCache _memoryCache;

        public OTPController(EmailService emailService, IMemoryCache memoryCache)
        {
            _emailService = emailService;
            _memoryCache = memoryCache;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            // Tạo mã OTP (6 chữ số ngẫu nhiên)
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu mã OTP vào MemoryCache với thời gian sống là 5 phút
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            _memoryCache.Set(request.Email, otpCode, cacheEntryOptions);

            // Gửi email chứa mã OTP
            var subject = "Your OTP Code";
            var body = $"Your OTP code is {otpCode}. It will expire in 5 minutes.";
            await _emailService.SendEmailAsync(request.Email, subject, body);

            return Ok(new { Message = "OTP sent successfully." });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] OtpVerificationRequest request)
        {
            if (!_memoryCache.TryGetValue(request.Email, out string storedOtp))
            {
                return BadRequest("OTP has expired or is invalid.");
            }

            if (storedOtp != request.OtpCode)
            {
                return BadRequest("Invalid OTP code.");
            }

            // Xóa OTP sau khi xác minh thành công
            _memoryCache.Remove(request.Email);

            return Ok("OTP verified successfully.");
        }
    }
}