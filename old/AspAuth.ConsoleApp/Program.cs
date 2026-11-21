
using AspAuth.Lib.Services;

Console.WriteLine("Hello, World!");

var server = "mx.atle.guru";
var from = "hello@atle.guru";
var pass = "xx";
var to = "atle@atle.guru";
var mail = new MailKitSender(server, 587, from, pass);

var res = await mail.SendMail(to, "hello dotnet 2", "from dotnet 2");
Console.WriteLine($"Mail Sent: {res}");
