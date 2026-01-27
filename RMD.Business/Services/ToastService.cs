using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMD.Business.Services
{

	/// <summary>
	/// ToastService is a singleton service which is using an event (OnShow) to notify a the toast component when it's supposed to be displayed.
	/// This service can be thought of as a broadcaster. The toast component then listens for OnShow to be invoked, displaying the toast.
	/// The enum is used by the component to decide which version of the toast is going to be displayed at given moments.
	/// </summary>

	public class ToastService
	{
		public event Action<string, ToastType>? OnShow;
		public void ShowToast(string message, ToastType type = ToastType.Info)
		{
			OnShow?.Invoke(message, type);
		}
	}

	public enum ToastType
	{
		Info,
		Warning,
		Error,
		Success
	}
}
