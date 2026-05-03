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
        LocationContextSnapshotEntity::class,
        LocationVisitEntity::class,
        CurrentAppStateEntity::class
    ],
    version = 5,
    exportSchema = false
)
abstract class MonitorDatabase : RoomDatabase() {
    abstract fun focusSessionDao(): FocusSessionDao
    abstract fun syncOutboxDao(): SyncOutboxDao
    abstract fun locationContextSnapshotDao(): LocationContextSnapshotDao
    abstract fun locationVisitDao(): LocationVisitDao
    abstract fun currentAppStateDao(): CurrentAppStateDao

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

        private val Migration3To4 = object : Migration(3, 4) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    """
                    CREATE TABLE IF NOT EXISTS location_visits (
                        id TEXT NOT NULL PRIMARY KEY,
                        deviceId TEXT NOT NULL,
                        locationKey TEXT NOT NULL,
                        latitude REAL NOT NULL,
                        longitude REAL NOT NULL,
                        coordinatePrecisionDecimals INTEGER NOT NULL,
                        firstCapturedAtUtcMillis INTEGER NOT NULL,
                        lastCapturedAtUtcMillis INTEGER NOT NULL,
                        durationMs INTEGER NOT NULL,
                        sampleCount INTEGER NOT NULL,
                        accuracyMeters REAL,
                        permissionState TEXT NOT NULL,
                        captureMode TEXT NOT NULL,
                        createdAtUtcMillis INTEGER NOT NULL,
                        updatedAtUtcMillis INTEGER NOT NULL
                    )
                    """.trimIndent()
                )
                db.execSQL(
                    """
                    CREATE INDEX IF NOT EXISTS index_location_visits_device_key_last
                    ON location_visits(deviceId, locationKey, lastCapturedAtUtcMillis)
                    """.trimIndent()
                )
                db.execSQL(
                    """
                    CREATE INDEX IF NOT EXISTS index_location_visits_device_time
                    ON location_visits(deviceId, firstCapturedAtUtcMillis, lastCapturedAtUtcMillis)
                    """.trimIndent()
                )
            }
        }

        private val Migration4To5 = object : Migration(4, 5) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL(
                    """
                    CREATE TABLE IF NOT EXISTS current_app_states (
                        clientStateId TEXT NOT NULL PRIMARY KEY,
                        packageName TEXT NOT NULL,
                        appLabel TEXT NOT NULL,
                        status TEXT NOT NULL,
                        observedAtUtcMillis INTEGER NOT NULL,
                        localDate TEXT NOT NULL,
                        timezoneId TEXT NOT NULL,
                        source TEXT NOT NULL,
                        createdAtUtcMillis INTEGER NOT NULL
                    )
                    """.trimIndent()
                )
                db.execSQL(
                    """
                    CREATE INDEX IF NOT EXISTS index_current_app_states_observed_client
                    ON current_app_states(observedAtUtcMillis, clientStateId)
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
                    .addMigrations(Migration1To2, Migration2To3, Migration3To4, Migration4To5)
                    .build()
                    .also { instance = it }
            }
        }
    }
}
