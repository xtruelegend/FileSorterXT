namespace FileSorterXT.Models;

public enum FileCategory
{
    Image,
    Document,
    Music,
    Video,
    Other
}

public enum ConfirmationMode
{
    Manual,
    AutoCountdown,
    PreviewOnly
}

public enum SortActionMode
{
    Move,
    Copy
}

public enum DuplicateDefinition
{
    FilenameOnly,
    FilenameAndSize,
    HashMatch,
    QuickAccurateModes
}

public enum DuplicateAction
{
    DoNotMove,
    MoveToDuplicatesFolder,
    MoveToCustomFolder
}

public enum KeepRule
{
    KeepNewest,
    KeepOldest,
    AskEachTime
}

public enum QuickAccurateMode
{
    Quick,
    Accurate
}
