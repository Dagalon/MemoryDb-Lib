namespace SqliteDB_Memory_Lib;

public static class EnumsSqliteMemory
{
    public enum Output
    {
        SUCCESS=1,
        DB_NOT_FOUND=2,
        PATH_NOT_FOUND=3,
        THERE_EXISTS_DATABASE=4,
        PATH_IS_NULL_OR_EMPTY=5,
        COLLECTION_NOT_FOUND=6,
        PATH_AND_ID_IS_NULL_OR_EMPTY=7,
        ERROR_TO_DEBUG = 8,
    }


}
