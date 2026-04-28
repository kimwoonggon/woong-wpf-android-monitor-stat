package com.woong.monitorstack.data.local

import androidx.room.Database
import androidx.room.RoomDatabase

@Database(
    entities = [FocusSessionEntity::class],
    version = 1,
    exportSchema = false
)
abstract class MonitorDatabase : RoomDatabase() {
    abstract fun focusSessionDao(): FocusSessionDao
}
