using HTTPlease;
using HTTPlease.Formatters;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DD.CloudControl.Client
{
	using Models.Directory;

	/// <summary>
	///		The CloudControl API client. 
	/// </summary>
	public sealed partial class CloudControlClient
		: IDisposable
	{
		/// <summary>
		///		Factory for <see cref="HttpClient"/>s used by the <see cref="CloudControlClient"/>. 
		/// </summary>
		static readonly ClientBuilder HttpClientBuilder = new ClientBuilder();

		/// <summary>
		///		 The HTTP client used to communicate with the CloudControl API.
		/// </summary>
		readonly HttpClient	_httpClient;

		/// <summary>
		///		Cached user account information. 
		/// </summary>
		UserAccount			_account;

		/// <summary>
		/// 	Has the client been disposed?
		/// </summary>
		/// <remarks>
		/// 	1 means disposed.
		/// </remarks>
		int					_isDisposed;

		/// <summary>
		///		Create a new <see cref="CloudControlClient"/>.
		/// </summary>
		/// <param name="httpClient">
		/// 	The HTTP client used to communicate with the CloudControl API.
		/// 
		/// 	Disposing the client will dispose this HTTP client.
		/// </param>
		/// <param name="account">
		/// 	Optional user account information to pre-populate the cache.
		/// </param>
		internal CloudControlClient(HttpClient httpClient, UserAccount account = null)
		{
			if (httpClient == null)
				throw new ArgumentNullException(nameof(httpClient));

			_httpClient = httpClient;
			_account = account;
		}

		/// <summary>
		/// 	Dispose of resources being used by the client.
		/// </summary>
		public void Dispose()
		{
			bool alreadyDisposed =
				Interlocked.Exchange(ref _isDisposed, 1) == 1;

			if (alreadyDisposed)
				return;

			_httpClient.Dispose();
		}

		/// <summary>
		/// 	Check if the client has been disposed.
		/// </summary>
		void CheckDisposed()
		{
			if (_isDisposed == 1)
				throw new ObjectDisposedException(nameof(CloudControlClient));
		}

		/// <summary>
		/// 	Reset the client, clearing all cached data.
		/// </summary>
		public void Reset()
		{
			CheckDisposed();

			_account = null;
		}

		/// <summary>
		/// 	The base address for the Cloud Control API.
		/// </summary>
		public Uri BaseAddress
		{
			get
			{
				CheckDisposed();

				return _httpClient.BaseAddress;
			}
		}

		/// <summary>
		///		Retrieve the user's account information. 
		/// </summary>
		/// <param name="cancellationToken">
		/// 	An optional <see cref="CancellationToken"/> that can be used to cancel the request.
		/// </param>
		/// <returns>
		/// 	The user account information.
		/// </returns>
		public Task<UserAccount> GetAccount(CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetAccount(false, cancellationToken);
		}

		/// <summary>
		///		Retrieve the user's account information. 
		/// </summary>
		/// <param name="refresh">
		/// 	Don't use cached account information.
		/// </param>
		/// <param name="cancellationToken">
		/// 	An optional <see cref="CancellationToken"/> that can be used to cancel the request.
		/// </param>
		/// <returns>
		/// 	The user account information.
		/// </returns>
		public async Task<UserAccount> GetAccount(bool refresh, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckDisposed();

			if (_account != null && !refresh)
				return _account;

			_account = await _httpClient
				.GetAsync(Requests.Directory.UserAccount, cancellationToken)
				.ReadAsAsync<UserAccount>();

			return _account;
		}

		/// <summary>
		///		Retrieve the user's organisation Id. 
		/// </summary>
		/// <param name="cancellationToken">
		/// 	An optional <see cref="CancellationToken"/> that can be used to cancel the request.
		/// </param>
		/// <returns>
		/// 	The user's organisation Id.
		/// </returns>
		async Task<Guid> GetOrganizationId(CancellationToken cancellationToken = default(CancellationToken))
		{
			UserAccount userAccount = await GetAccount(cancellationToken);

			return userAccount.OrganizationId;
		}

		/// <summary>
		///		 Create a new <see cref="CloudControlClient"/>.
		/// </summary>
		/// <param name="baseUri">
		/// 	The base URI for the CloudControl API.
		/// </param>
		/// <param name="userName">
		/// 	The CloudControl user name.
		/// </param>
		/// <param name="password">
		/// 	The CloudControl password.
		/// </param>
		/// <returns>
		/// 	The configured <see cref="CloudControlClient"/>.
		/// </returns>
		public static CloudControlClient Create(Uri baseUri, string userName, string password)
		{
			if (baseUri == null)
				throw new ArgumentNullException(nameof(baseUri));

			return Create(baseUri,
				credentials: new NetworkCredential(userName, password)
			);
		}

		/// <summary>
		///		 Create a new <see cref="CloudControlClient"/>.
		/// </summary>
		/// <param name="baseUri">
		/// 	The base URI for the CloudControl API.
		/// </param>
		/// <param name="credentials">
		/// 	The network credentials used to authenticate to CloudControl.
		/// </param>
		/// <returns>
		/// 	The configured <see cref="CloudControlClient"/>.
		/// </returns>
		public static CloudControlClient Create(Uri baseUri, NetworkCredential credentials)
		{
			if (baseUri == null)
				throw new ArgumentNullException(nameof(baseUri));

			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			return new CloudControlClient(
				HttpClientBuilder.CreateClient(baseUri, new HttpClientHandler
				{
					Credentials = credentials,
					PreAuthenticate = true
				})
			);
		}
	}
}