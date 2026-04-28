package com.woong.monitorstack.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase

@Database(
    entities = [FocusSessionEntity::class],
    version = 1,
    exportSchema = false
)
abstract class MonitorDatabase : RoomDatabase() {
    abstract fun focusSessionDao(): FocusSessionDao

    companion object {
        @Volatile
        private var instance: MonitorDatabase? = null

        fun getInstance(context: Context): MonitorDatabase {
            return instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(
                    context.applicationContext,
                    MonitorDatabase::class.java,
                    "woong-monitor.db"
                ).build().also { instance = it }
            }
        }
    }
}
