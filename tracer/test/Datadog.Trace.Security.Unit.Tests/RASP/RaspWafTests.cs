// <copyright file="RaspWafTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using Datadog.Trace.AppSec;
using Datadog.Trace.AppSec.Rcm;
using Datadog.Trace.AppSec.Waf;
using Datadog.Trace.AppSec.Waf.ReturnTypes.Managed;
using Datadog.Trace.Security.Unit.Tests.Utils;
using Datadog.Trace.TestHelpers.FluentAssertionsExtensions.Json;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using FluentAssertions;
using Xunit;
using Action = Datadog.Trace.AppSec.Rcm.Models.Asm.Action;

namespace Datadog.Trace.Rasp.Unit.Tests;

public class RaspWafTests : WafLibraryRequiredTest
{
    [Fact]
    public void SendEmailUnsafe()
    {
        string name = "You have an unpaid bill                                                                                                                                                                                                                                                                                        ";
        // string lastName = "<style>*{color: transparent;} #bill{color: #000;} #bill a{color: blue;}</style> <p id=\"bill\">You have an unpaid bill, please, follow the instructions <a href=\"https://localhost/malicious_site\">here</a> or you will incur an additional penalty.";
        string lastName = "<style>*{color: transparent;} #bill{color: #000;} #bill a{color: blue;}</style> <p id=\"bill\">You have an unpaid bill, please, follow the instructions <a href=\"https://localhost/evilsite\">here</a> or you will incur an additional penalty.</p>";

        SendMail(name, lastName);
    }

    [Fact]
    public void SendEmailLegit()
    {
        // string name = "<style>*{color: transparent;} #bill{color: #000;} #bill a{color: blue;}</style> <p id=\"bill\">Your bill is due, <a href=\"https://malicious_site/paynow\">Pay now</a> or we will charge a late fee.</p>";
        string name = "John";
        string lastName = "Smith";

        SendMail(name, lastName);
    }

    private static void SendMail(string firstName, string lastName)
    {
        string contentHtml = $"Hi {firstName} {lastName}, <br />" +
            "We appreciate you subscribing to our newsletter. To complete your subscription, kindly click the link below. <br />" +
            "<a href=\"https://localhost/confirm?token=???\">Complete your subscription</a>";

        var subject = $"{firstName}, welcome!";
        var smtpUsername = "j@hotmail.com";
        string email = smtpUsername;
        var smtpPassword = "jj";
        var server = "smtp-mail.outlook.com";
        int port = 587;

        var mailMessage = new System.Net.Mail.MailMessage();
        mailMessage.From = new System.Net.Mail.MailAddress(smtpUsername);
        mailMessage.To.Add(email);
        mailMessage.Subject = subject;
        mailMessage.Body = contentHtml;
        mailMessage.IsBodyHtml = true; // Set to true to indicate that the body is HTML

        var client = new SmtpClient(server, port)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true,
            Timeout = 10000
        };
        client.Send(mailMessage);
    }
}
