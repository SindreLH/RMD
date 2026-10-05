namespace RMD.GUI.Pages.Components
{
	/// <summary>One dropdown in an <see cref="ArtistPicker"/>.</summary>
	public sealed class ArtistPickerRow
	{
		public Guid Key { get; } = Guid.NewGuid();
		public int? SelectedId { get; set; }
	}
}
