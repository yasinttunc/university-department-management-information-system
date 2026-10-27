-- University Department Management Information System: database schema (SQLite).
-- Applied automatically by DatabaseInitializer when the database file is empty.

CREATE TABLE users (
  id            INTEGER PRIMARY KEY,
  username      TEXT NOT NULL UNIQUE COLLATE NOCASE,
  password_hash TEXT NOT NULL,
  role          TEXT NOT NULL CHECK (role IN ('Admin', 'Staff')),
  department    TEXT NOT NULL
);

CREATE TABLE staff (
  id         INTEGER PRIMARY KEY,
  title      TEXT NOT NULL CHECK (title IN ('Prof.', 'Assoc. Prof.', 'Asst. Prof.', 'Dr.', 'Lecturer')),
  first_name TEXT NOT NULL,
  last_name  TEXT NOT NULL,
  email      TEXT NOT NULL UNIQUE
);

CREATE TABLE students (
  id             INTEGER PRIMARY KEY,
  student_number TEXT NOT NULL UNIQUE,
  first_name     TEXT NOT NULL,
  last_name      TEXT NOT NULL,
  national_id    TEXT NOT NULL UNIQUE CHECK (length(national_id) = 9),
  email          TEXT NOT NULL UNIQUE,
  phone          TEXT,
  address        TEXT,
  programme      TEXT NOT NULL,
  year_of_study  INTEGER NOT NULL CHECK (year_of_study BETWEEN 1 AND 4),
  enrolment_year INTEGER NOT NULL
);

CREATE TABLE courses (
  id         INTEGER PRIMARY KEY,
  code       TEXT NOT NULL UNIQUE,
  title      TEXT NOT NULL,
  credits    INTEGER CHECK (credits >= 0),
  ects       INTEGER CHECK (ects >= 0),
  year_level INTEGER NOT NULL CHECK (year_level BETWEEN 1 AND 4)
);

CREATE TABLE sections (
  id             INTEGER PRIMARY KEY,
  course_id      INTEGER NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
  section_number TEXT NOT NULL,
  room           TEXT,
  capacity       INTEGER NOT NULL CHECK (capacity >= 0),
  lecturer_id    INTEGER REFERENCES staff(id) ON DELETE SET NULL,
  UNIQUE (course_id, section_number)
);

CREATE TABLE timetable_slots (
  id          INTEGER PRIMARY KEY,
  section_id  INTEGER NOT NULL REFERENCES sections(id) ON DELETE CASCADE,
  day_of_week INTEGER NOT NULL CHECK (day_of_week BETWEEN 1 AND 5),
  start_time  TEXT NOT NULL,
  end_time    TEXT NOT NULL,
  UNIQUE (section_id, day_of_week, start_time)
);

CREATE TABLE enrolments (
  section_id INTEGER NOT NULL REFERENCES sections(id) ON DELETE CASCADE,
  student_id INTEGER NOT NULL REFERENCES students(id) ON DELETE CASCADE,
  PRIMARY KEY (section_id, student_id)
);

CREATE TABLE advisor_assignments (
  student_id INTEGER PRIMARY KEY REFERENCES students(id) ON DELETE CASCADE,
  advisor_id INTEGER NOT NULL REFERENCES staff(id),
  evaluation TEXT CHECK (evaluation IN ('Successful', 'Average', 'Unsuccessful'))
);

CREATE TABLE calendar_events (
  id          INTEGER PRIMARY KEY,
  event_date  TEXT NOT NULL,
  title       TEXT NOT NULL,
  description TEXT
);

CREATE TABLE reminders (
  id        INTEGER PRIMARY KEY,
  remind_on TEXT NOT NULL,
  note      TEXT NOT NULL
);

CREATE INDEX idx_sections_lecturer  ON sections(lecturer_id);
CREATE INDEX idx_slots_section      ON timetable_slots(section_id);
CREATE INDEX idx_enrolments_student ON enrolments(student_id);
CREATE INDEX idx_advising_advisor   ON advisor_assignments(advisor_id);
CREATE INDEX idx_events_date        ON calendar_events(event_date);

PRAGMA user_version = 1;
