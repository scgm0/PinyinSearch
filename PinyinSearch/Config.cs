namespace PinyinSearch;

public class Config {
	public bool EnableFuzzyInitials { get; set; } = true;
	public bool EnableFuzzyFinals { get; set; } = true;
	public bool ExactMatchForHanzi { get; set; } = true;
}