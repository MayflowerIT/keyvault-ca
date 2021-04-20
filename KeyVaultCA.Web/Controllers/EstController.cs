﻿using KeyVaultCa.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KeyVaultCA.Web.Controllers
{
    [ApiController]
    [Route(".well-known/est")]
    public class EstController : ControllerBase
    {
        private const string PKCS7_MIME_TYPE = "application/pkcs7-mime";
        private const string PKCS10_MIME_TYPE = "application/pkcs10";

        private readonly ILogger<EstController> _logger;
        private readonly IKeyVaultCertificateProvider _keyVaultCertProvider;
        private readonly CAConfuguration _confuguration;

        public EstController(ILogger<EstController> logger, IKeyVaultCertificateProvider keyVaultCertProvider, CAConfuguration confuguration)
        {
            _logger = logger;
            _keyVaultCertProvider = keyVaultCertProvider;
            _confuguration = confuguration;
        }

        [HttpGet]
        [Authorize]
        [Route("cacerts")]
        public async Task<IActionResult> GetCACertsAsync()
        {
            var caCerts = new List<X509Certificate2>();

            foreach (var issuerName in _confuguration.CACerts)
            {
                var cert = await _keyVaultCertProvider.GetCertificateAsync(issuerName).ConfigureAwait(false);

                if (cert != null)
                {
                    caCerts.Add(cert);
                }
            }

            var pkcs7 = EncodeCertificateAsPem(caCerts.ToArray());
            return Content(pkcs7, PKCS7_MIME_TYPE);
        }

        [HttpPost]
        [Authorize]
        [Route("simpleenroll")]
        [Consumes(PKCS10_MIME_TYPE)]
        public async Task<IActionResult> EnrollAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var cleanedUpBody = CleanUpAsn1Structure(body);

            var cert = await _keyVaultCertProvider.SigningRequestAsync(Convert.FromBase64String(cleanedUpBody), _confuguration.IssuingCA);

            var pkcs7 = EncodeCertificateAsPem(new[] { cert });
            return Content(pkcs7, PKCS7_MIME_TYPE);
        }

        private string EncodeCertificateAsPem(X509Certificate2 [] certs)
        {
            var collection = new X509Certificate2Collection(certs);
            var data = collection.Export(X509ContentType.Pkcs7);

            var builder = new StringBuilder();
            builder.AppendLine(Convert.ToBase64String(data));

            return builder.ToString();
        }

        private string CleanUpAsn1Structure(string raw)
        {
            var tokens = raw.Split(Environment.NewLine);
            return string.Join("", tokens.Skip(1).Take(tokens.Length - 3));
        }
    }
}