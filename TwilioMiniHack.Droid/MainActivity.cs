using Android.App;
using Android.Widget;
using Android.OS;

using System.Threading.Tasks;
using System.Net.Http;
using System.Json;
using System;
using System.Collections.Generic;

using Twilio.Common;
using Twilio.IPMessaging;

namespace TwilioMiniHack.Droid
{
	[Activity(Label = "#general", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity, IPMessagingClientListener, IChannelListener, ITwilioAccessManagerListener
	{
		internal const string TAG = "TWILIO";

		Button sendButton;
		EditText textMessage;
		ListView listView;
		MessagesAdapter adapter;

		ITwilioIPMessagingClient client;
		IChannel generalChannel;

		// ...

		protected async override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			this.ActionBar.Subtitle = "logging in...";

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			sendButton = FindViewById<Button>(Resource.Id.sendButton);
			textMessage = FindViewById<EditText>(Resource.Id.messageTextField);
			listView = FindViewById<ListView>(Resource.Id.listView);

			adapter = new MessagesAdapter(this);
			listView.Adapter = adapter;

			//client = TwilioIPMessagingSDK.CreateIPMessagingClientWithAccessManager
		}

	}

		class MessagesAdapter : BaseAdapter<IMessage>
		{
			public MessagesAdapter(Activity parentActivity)
			{
				activity = parentActivity;
			}

			List<IMessage> messages = new List<IMessage>();
			Activity activity;

			public void AddMessage(IMessage msg)
			{
				lock (messages)
				{
					messages.Add(msg);
				}

				activity.RunOnUiThread(() =>
					NotifyDataSetChanged());
			}

			public override long GetItemId(int position)
			{
				return position;
			}

			public override Android.Views.View GetView(int position, Android.Views.View convertView, Android.Views.ViewGroup parent)
			{
				var view = convertView as LinearLayout ?? activity.LayoutInflater.Inflate(Resource.Layout.MessageItemLayout, null) as LinearLayout;
				var msg = messages[position];

				view.FindViewById<TextView>(Resource.Id.authorTextView).Text = msg.Author;
				view.FindViewById<TextView>(Resource.Id.messageTextView).Text = msg.MessageBody;

				return view;
			}

			public override int Count { get { return messages.Count; } }
			public override IMessage this[int index] { get { return messages[index]; } }
		}


	}



