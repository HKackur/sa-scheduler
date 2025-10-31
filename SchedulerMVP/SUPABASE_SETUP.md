# Supabase Setup Instructions

## 1. Get your Supabase connection details

1. Go to your Supabase dashboard
2. Click on your project
3. Go to "Settings" → "Database"
4. Find the "Connection string" section
5. Copy the connection string (it should look like: `postgresql://postgres:[YOUR-PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres`)

## 2. Update the connection string

1. Open `appsettings.json`
2. Replace the connection string in `DefaultConnection` with your actual Supabase connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.[YOUR-PROJECT-REF].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=[YOUR-PASSWORD]"
  },
  ...
}
```

## 3. Run the application

The application will automatically:
- Connect to your Supabase database
- Create all tables
- Seed with initial data
- Sync all changes in real-time

## 4. Verify in Supabase

1. Go to your Supabase dashboard
2. Click on "Table Editor"
3. You should see all your tables: Groups, Places, Areas, Leafs, etc.
4. All data will sync automatically when you use the app

## Benefits

- ✅ Real-time database access
- ✅ Easy to inspect data
- ✅ Automatic backups
- ✅ Better performance
- ✅ No more local SQLite files
