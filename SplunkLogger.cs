﻿using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SplunkClient
{
	/*
	* Logger is a portable class that easily sends data
	* to Splunk over HTTP/HTTPS providing great abstraction
	* for developers logging mobile data to Splunk */
	public class SplunkLogger
	{
		private string level;
		private string uri;
		private HttpClient client; 
		private bool sslEnabled;
        	private string sourcetype;

		// Keeps track of severity levels
		private List<string> levels;

        	// Keeps track of any errors, and their respective events
        	private List<KeyValuePair<string, string>> errors;

		/*
		* Constructs a SplunkLogger with (most importantly), URI and Http Event Collector token */
		public SplunkLogger (string newUri, string token, bool ssl)
		{
			client = new HttpClient ();
			client.DefaultRequestHeaders.Authorization = 
				new System.Net.Http.Headers.AuthenticationHeaderValue("Splunk", token);
			this.uri = newUri;

			// add severity levels for SplunkLogger
			levels = new List<string> ();
			levels.Add ("ERROR");
			levels.Add ("INFO");
			levels.Add ("OFF");
			levels.Add ("VERBOSE");
			levels.Add ("WARNING");
			this.level = "INFO";
            		this.sourcetype = "Mobile Application";
            		errors = new List<KeyValuePair<string, string>>();

			sslEnabled = ssl;
			/*
			if (sslEnabled) {
				System.Net.ServicePointManager.ServerCertificateValidationCallback += 
					(sender, cert, chain, sslPolicyErrors) => true;
			}
			*/
		}

		/*
		* Tells SplunkLogger whether or not to enable SSL */
		public void EnableSSL ()
		{
			/* Should enable SSL in DIFFERENT WAY if Splunk deployment isn't self signed
			 * otherwise, this code will automatically validate server cert
			if (!sslEnabled) {
				System.Net.ServicePointManager.ServerCertificateValidationCallback += 
					(sender, cert, chain, sslPolicyErrors) => true;
			}
			*/
			sslEnabled = true;
		}

		/*
		* Changes the severity level of this SplunkLogger */
		public void SetLevel (string newLevel)
		{
        		string levelVal = newLevel.ToUpper();
			if (levels != null && levels.Contains(levelVal)) {
				this.level = levelVal;
			}
		}

        	/*
        	* Changes the sourcetype of this SplunkLogger */
        	public void SetSourcetype(string type)
        	{
        		this.sourcetype = type;
        	}

        	/*
		* Logs a string message/event to Splunk's Http Event Collector 
        	* Sequential, less verbose, swallows exceptions */
        	public void Log (string message)
		{
            		HandleLog(message, false);
		}

		/*
		* Logs a string message/event to Splunk's Http Event Collector, Async */
		async public void LogAsync (string message)
		{
            		/*
            		if (!string.Equals(this.level, "OFF"))
            		{
                		var resp = await client.PostAsync(this.uri, GetHttpContent(message));
                		resp.EnsureSuccessStatusCode();
            		}
            		*/
            		await HandleLog(message, true);
		}

	        async private Task HandleLog(string message, bool async)
	        {
	            if (string.Equals(this.level, "OFF"))
	            {
	                errors.Add(new KeyValuePair<string, string>(
	                    "Cannot send events when SplunkLogger is turned off", message));
	                return;
	            }
	            try
	            {
	                var content = GetHttpContent(message);
	                var response = async ? await client.PostAsync(this.uri, content) :
	                    client.PostAsync(this.uri, content).Result;
	                response.EnsureSuccessStatusCode();
	            }
	            catch (Exception e)
	            {
	                errors.Add(new KeyValuePair<string, string>(e.Message, message));
	            }
	        }
	
	        /*
	        * returns a JSON populated HttpContent object with the given message */
	        private HttpContent GetHttpContent(string message)
	        {
	            string JSONstr = "{\"event\":{\"message\":\"" + message +
	                "\", \"severity\":\"" + this.level + "\"}, \"sourcetype\":\"" + this.sourcetype + "\"}";
	            return new StringContent(JSONstr);
	        }
	
	        /*
	        * resends all events that caused exceptions */
	        public void ResendErrors()
	        {
	        	while (errors.Count > 0)
	        	{
	                	foreach (var error in errors)
	                	{
	                    		LogAsync(error.Value);
	                	}
	            	}
	        }
	}
}
