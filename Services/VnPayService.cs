using System;
using System.Globalization;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace FastFoodShop.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(decimal amount, string orderId, string orderDescription, string returnUrl, string ipAddress);
        PaymentResponseModel ProcessPaymentResponse(IQueryCollection query);
    }

    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(decimal amount, string orderId, string orderDescription, string returnUrl, string ipAddress)
        {
            var vnpay = new VnPayLibrary();
            
            var vnp_TmnCode = _configuration["VnPay:TmnCode"];
            var vnp_HashSecret = _configuration["VnPay:HashSecret"];
            var vnp_Url = _configuration["VnPay:Url"];

            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString()); // Số tiền * 100
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", orderDescription);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", orderId);
            
            // Thêm thời gian hết hạn (15 phút)
            vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            var paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }

        public PaymentResponseModel ProcessPaymentResponse(IQueryCollection query)
        {
            var vnpay = new VnPayLibrary();
            var vnp_HashSecret = _configuration["VnPay:HashSecret"];

            foreach (var (key, value) in query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value);
                }
            }

            var orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            var vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_SecureHash = query.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value;
            var orderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            var amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;

            var checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            return new PaymentResponseModel
            {
                Success = checkSignature && vnp_ResponseCode == "00",
                PaymentMethod = "VnPay",
                OrderDescription = orderInfo,
                OrderId = orderId.ToString(),
                PaymentId = vnpayTranId.ToString(),
                TransactionId = vnpayTranId.ToString(),
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode,
                Amount = amount
            };
        }
    }

    public class PaymentResponseModel
    {
        public bool Success { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
        public long Amount { get; set; }
    }
}