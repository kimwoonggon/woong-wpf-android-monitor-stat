package com.woong.monitorstack.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase

@Database(
    entities = [
        FocusSessionEntity::class,
        SyncOutboxEntity::class,
        LocationContextSnapshotEntity::class
    ],
    version = 3,
    exportSchema = false
)
abstract class MonitorDatabase : RoomDatabase() {
    abstract fun focusSessionDao(): FocusSessionDao
    abstract fun syncOutboxDao(): SyncOutboxDao
    abstract fun locationContextSnapshotDao(): LocationContextSnapshotDao

    companion object {
        @Volatile
        private var instance: MonitorDatabase? = null

        private val Migration1To2 = object : Migration(1, 2) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    """
                    CREATE TABLE IF NOT EXISTS sync_outbox (
                        clientItemId TEXT NOT NULL PRIMARY KEY,
                        aggregateType TEXT NOT NULL,
                        payloadJson TEXT NOT NULL,
                        status TEXT NOT NULL,
                        retryCount INTEGER NOT NULL,
                        lastError TEXT,
                        createdAtUtcMillis INTEGER NOT NULL,
                        updatedAtUtcMillis INTEGER NOT NULL
                    )
                    """.trimIndent()
                )
            }
        }

        private val Migration2To3 = object : Migration(2, 3) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    """
                    CREATE TABLE IF NOT EXISTS location_context_snapshots (
                        id TEXT NOT NULL PRIMARY KEY,
                        deviceId TEXT NOT NULL,
                        capturedAtUtcMillis INTEGER NOT NULL,
                        latitude REAL,
                        longitude REAL,
                        accuracyMeters REAL,
                        permissionState TEXT NOT NULL,
                        captureMode TEXT NOT NULL,
                        createdAtUtcMillis INTEGER NOT NULL
                    )
                    """.trimIndent()
                )
            }
        }

        fun getInstance(context: Context): MonitorDatabase {
            return instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(
                    context.applicationContext,
                    MonitorDatabase::class.java,
                    "woong-monitor.db"
                )
                    .addMigrations(Migration1To2, Migration2To3)
                    .build()
                    .also { instance = it }
            }
        }
    }
}
