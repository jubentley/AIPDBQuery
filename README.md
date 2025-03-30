![og-image](memory-wars-screenshot.png)

<i>who will win the memory wars?</i>

<i>tldr: x86 or AOT and never, ever x64!</i>

# AIPDBQuery, AbuseIPDB Query CLI Tool

This console application queries the AbuseIPDB API v2 to check an IP address for potential abuse reports. It returns the Abuse Confidence Score, ISP, Usage Type, and Country Code for the specified IP.

The tool uses a persistent HttpClient instance, pulls the API key from a local 'abuseipdbkey.config' file, and features color-coded terminal output for readability.

## Prerequisites
* .NET 6 or later if not using the AOT build
* AbuseIPDB API v2 Key (https://www.abuseipdb.com/)
  * Needs a API key from AbuseIPDB, free signup, no charges, no commitments, 1000 Queries per day.
  * 1000 queries per day can be increased to 5000 (still free) if your a Webmaster (host a key on your website) and Contributor (display the Abuse Contributor badge on one of your websites [jbn.ai?abuse](https://jbn.ai/?abuse)).

