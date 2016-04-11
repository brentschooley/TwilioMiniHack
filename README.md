
Twilio Mini Hack
===
Welcome, fine adventurer to the Twilio Mini Hack.  This simple hack will introduce you to [Twilio IP Messaging](https://twilio.com/ip-messaging) which allows you to build chat functionality into your native iOS and Android applications without having to build and scale a backend infrastructure.  

Once you've completed this minihack in a short period of time you'll have a fully functioning chat application that works on the web, iOS and Android.

Sounds amazing, doesn't it?  We think so too!  

We think you'll be able to complete this mini hack in 10 minutes.  If you get stuck or have any questions, no problem.  Head over to the Twilio booth and we'll be happy to walk through some code with you.

Alright.  With the intro out of the way, let’s get building!

### Mini Hack Requirements ###

Developers using Macs can build either the iOS or Android versions of this hack.  You'll need to make sure you have the following installed:

- a copy of Xamarin Studio with either Xamarin.iOS or Xamarin.Android

Windows developers will only be able to build the Android version of the hack (unless they are running Windows in a VM on a Mac).  You'll need to make sure you have the following:

- Visual Studio 2015
- Xamarin for Windows with Xamarin.Android
- Administrator access to your development machine.

Of course if you have any questions about your specific development environment, head over to the Twilio booth and we'll be happy to help you out.

Getting Started
===
To get started you'll need to set up a bit of infrastructure.

As you might expect, the first thing you'll need is a Twilio account.  Don't worry, trial accounts are free, so if you don't already have one, [head on over to the Twilio website](https://www.twilio.com/try-twilio) and sign up.  I'll wait right here while you do it.

You're back! Fantastic. Let’s move on to the second thing you'll need.  

To make all of this work we will need some server code to generate access tokens. An access token tells Twilio who a chat user is and what they can and can't do within the IP Messaging service. You can find out more about access tokens [here](https://www.twilio.com/docs/api/ip-messaging/guides/identity).

Head over to [this guide](https://github.com/TwilioDevEd/ipm-quickstart-csharp) and follow the instructions to get the ASP.NET version of our quickstart working on your machine. If you'd like to use a different backend language, you can find a full list of quickstart servers [here](https://www.twilio.com/docs/api/ip-messaging/guides/quickstart-js).

Once you have it set up correctly, open it in your browser and you should be looking at a chat application. You've been granted an access token by the server and assigned a random username. We'll use this same server infrastructure to request a token for our mobile application so keep it running and keep the URL handy. I'll refer to this URL later as `YOUR_TOKEN_SERVER_URL`.

Now it's time to choose your adventure. Open up the mini hack solution and jump to the platform you'd like to build your chat application in:

- [iOS](#ios)
- [Android](#android)

iOS
====
<a name="ios"></a>Alright iOS ninja, are you ready to build a chat application?  Fantastic!

Start by finding the iOS project we've provided in the hack solution:

![iOS Project Highlight](http://i.imgur.com/6rGy7cn.png)

This project has the main user interface objects for our chat app already created in `Main.storyboard` and some basic keyboard handling code in `ViewController.cs`. It also contains a table view cell that we'll use to display chat messages. What we'll do over the next few minutes is light up those UI objects with Twilio IP Messaging.

To get started, let's add the Twilio IP Messaging NuGet packages. Right-click on the `Packages` node inside the `TwilioMiniHack.iOS` project and select `Add packages...`:

![Add packages](http://i.imgur.com/PVTIv78.png)

Search for "Twilio IP Messaging" in the `Add Packages` dialog and check the boxes next to "Twilio IP Messaging for Xamarin" and "Twilio Common Library for Xamarin" and click the `Add Package` button in the bottom-right:

![Packages dialog](http://i.imgur.com/x6M75X3.png)

Now that we have Twilio IP Messaging in our project, let's add the following `using` statements to the top of ViewController.cs:

```csharp
// For IP Messaging
using Twilio.Common;
using Twilio.IPMessaging;
```

In `ViewController.cs`, let's configure the `ViewController` it so that it implements the following interfaces:

```csharp
public partial class ViewController : UIViewController, ITwilioIPMessagingClientDelegate, IUITextFieldDelegate, ITwilioAccessManagerDelegate
{
  // ...
}
```

`ITwilioIPMessagingClientDelegate` handles the events we receive from the IP Messaging service while `ITwilioAccessManagerDelegate` is used to coordinate authentication events with the service. We'll add the methods to implement these interfaces as we go along.

Since this is a chat app and we'll be doing a lot of work with messages, let's create a class that will help manage them for our table view. This class will be a subclass of `UITableViewSource` so that we can not only store our `Message` objects but also provide our table view with the methods it needs to render them.

Create a new class and call it `MessagesDataSource`. Replace the template code in this class with the following:

```csharp
using System;
using System.Collections.Generic;
using Twilio.IPMessaging;
using UIKit;

class MessagesDataSource : UITableViewSource
{
		public List<Message> Messages { get; private set; } = new List<Message>();
}
```

We'll need the ability to add messages to the list as they come in so let's add that to `MessagesDataSource`:

```csharp
public void AddMessage (Message msg)
{
		Messages.Add (msg);
}
```

Finally, we need the `NumberOfSections`, `RowsInSection` and `GetCell` method overrides that configure our table view to display the messages. Add these to `MessagesDataSource`:

```csharp
public override nint NumberOfSections(UITableView tableView)
{
	return 1;
}

public override nint RowsInSection(UITableView tableView, nint section)
{
	return Messages.Count;
}

public override UITableViewCell GetCell (UITableView tableView, Foundation.NSIndexPath indexPath)
{
    var message = Messages [indexPath.Row];

    var cell = tableView.DequeueReusableCell ("MessageCell") as MessageCell;
    cell.Message = message;
    cell.SetNeedsUpdateConstraints ();
    cell.UpdateConstraintsIfNeeded ();

    return cell;
}
```

The `GetCell` method uses the `MessageCell` class that is already provided in the mini hack solution.

The table view data source is good to go so let's populate our table view. Back in `ViewDidLoad` of the `ViewController.cs`, create a `MessagesDataSource` object and set it as the table source. While we're here we'll also provide the table view with some row dimension information:

```csharp
MessagesDataSource dataSource;

public async override void ViewDidLoad ()
{
    base.ViewDidLoad ();
    // Perform any additional setup after loading the view, typically from a nib.

    dataSource = new MsgsDataSource ();
    tableView.Source = dataSource;
    tableView.RowHeight = UITableView.AutomaticDimension;
    tableView.EstimatedRowHeight = 70;
}
```

Our table view is ready to go, now we just need to connect to Twilio IP Messaging and load it up.

### Connecting to Twilio IP Messaging

First, let's add some instance variables to our `ViewController` class to keep track of IP Messaging related things:

```csharp
// Our chat client
TwilioIPMessagingClient twilio;
// The channel we'll chat in
Channel generalChannel;
// Our username when we connect
string identity;
```

Now let's add a method that will fetch an access token from our server:

```csharp
async Task<string> GetToken ()
{
    var deviceId = UIDevice.CurrentDevice.IdentifierForVendor.AsString ();

    var tokenEndpoint = $"https://{{your server URL and port}}/token.php?device={deviceId}";

    var http = new HttpClient ();
    var data = await http.GetStringAsync (tokenEndpoint);

    var json = JsonObject.Parse (data);
    // Set the identity for use later, this is our username
	  identity = json ["identity"]?.ToString ()?.Trim ('"');

	  return json["token"]?.ToString ()?.Trim ('"');
}
```

We pass in the device ID as a unique identifier and we're returned a token that includes our identity. Excellent, now let's go back to `ViewDidLoad` and create the IP Messaging client using the token. Add the following code to `ViewDidLoad`:

```csharp
var token = await GetToken ();
var accessManager = TwilioAccessManager.Create (token, this);
client = TwilioIPMessagingClient.Create (accessManager, this);
```

We use the returned token to create a `TwilioAccessManager` and then use that to create an IP Messaging client. We set our view controller as the delegate so we can handle the various delegate methods the `TwilioIPMessagingClient` needs to function. Now would be a great time to implement the `ITwilioAccessManager` interface. Add the following code to the `ViewController` class:

```csharp
[Foundation.Export("accessManagerTokenExpired:")]
public void TokenExpired(Twilio.Common.TwilioAccessManager accessManager)
{
	Console.WriteLine("token expired");
}

[Foundation.Export("accessManager:error:")]
public void Error(Twilio.Common.TwilioAccessManager accessManager, Foundation.NSError error)
{
	Console.WriteLine("access manager error");
}
```

Now let's use the IP Messaging client to get a list of channels and either join the `general` channel if it already exists or create it if it doesn't:

```csharp
client.GetChannelsList ((result, channels) => {
    generalChannel = channels.GetChannelWithUniqueName ("general");

	if (generalChannel != null)
	{
		generalChannel.Join(r =>
		{
			Console.WriteLine("successfully joined general channel!");
		});
	}
	else
	{
		var options = new NSDictionary("TWMChannelOptionFriendlyName", "General Chat Channel", "TWMChannelOptionType", 0);

		channels.CreateChannel(options, (creationResult, channel) => {
			if (creationResult.IsSuccessful())
			{
				generalChannel = channel;
				generalChannel.Join(r => {
					generalChannel.SetUniqueName("general", res => { });
				});
			}
		});
	}

});
```

Next, head into `Main.storyboard` and double-click the `SEND` button in the bottom right to generate a click event for the button. Use this as the code for the button's Touch Up Inside event:

```csharp
partial void SendButton_TouchUpInside (UIButton sender)
{
    var msg = generalChannel.Messages.CreateMessage (messageTextField.Text);
    sendButton.Enabled = false;
    generalChannel.Messages.SendMessage(msg, r => {

        BeginInvokeOnMainThread (() => {
            messageTextField.Text = string.Empty;
            sendButton.Enabled = true;
        });

    });
}
```

This button will allow us to send messages but what about when we receive messages in the channel? Let's add some code to handle that now. When a message is sent to the channel we'll use a method that handles the `ipMessagingClient:channel:messageAdded:` delegate method to load it into the datasource for our Messages and reload the table view:

```csharp
[Foundation.Export("ipMessagingClient:channel:messageAdded:")]
public void MessageAdded(TwilioIPMessagingClient client, Channel channel, Message message)
{
	dataSource.AddMessage(message);
	tableView.ReloadData();
	if (dataSource.Messages.Count > 0)
	{
		ScrollToBottomMessage();
	}
}


public void ScrollToBottomMessage()
{
	if (dataSource.Messages.Count == 0)
	{
		return;
	}

	var bottomIndexPath = NSIndexPath.FromRowSection(this.tableView.NumberOfRowsInSection(0) - 1, 0);
	this.tableView.ScrollToRow(bottomIndexPath, UITableViewScrollPosition.Bottom, true);
}
```

With this in place we can send and receive messages on the general channel and have a functioning chat app in iOS! Explore the [Twiliio Docs](http://twilio.com/docs/api/ip-messaging) to find out what else you can do with your application. Show your completed application to a Xamarin to get credit for this mini hack. If you want, you can continue on to the Android implementation.
