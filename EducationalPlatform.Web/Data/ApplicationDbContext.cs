using EducationalPlatform.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        public DbSet<Assistant> Assistants { get; set; }

        public DbSet<Year> Years { get; set; }

        public DbSet<Group> Groups { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<AssistantYear> AssistantYears { get; set; }

        public DbSet<GroupSchedule> GroupSchedules { get; set; }

        public DbSet<ClassSession> ClassSessions { get; set; }

        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        public DbSet<Training> Trainings { get; set; }

        public DbSet<TrainingAssignment> TrainingAssignments { get; set; }

        public DbSet<TrainingRecord> TrainingRecords { get; set; }

        public DbSet<TrainingAttempt> TrainingAttempts { get; set; }

        public DbSet<Exam> Exams { get; set; }

        public DbSet<ExamAssignment> ExamAssignments { get; set; }

        public DbSet<ExamResult> ExamResults { get; set; }

        public DbSet<Quiz> Quizzes { get; set; }

        public DbSet<QuizAssignment> QuizAssignments { get; set; }

        public DbSet<QuizAttempt> QuizAttempts { get; set; }

        public DbSet<Post> Posts { get; set; }

        public DbSet<PostAttachment> PostAttachments { get; set; }

        public DbSet<RegistrationRequest> RegistrationRequests { get; set; }

        public DbSet<Conversation> Conversations { get; set; }

        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<AttendanceSettings> AttendanceSettings { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Student -> ApplicationUser
            builder.Entity<Student>()
                .HasOne(s => s.ApplicationUser)
                .WithOne()
                .HasForeignKey<Student>(s => s.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Assistant -> ApplicationUser
            builder.Entity<Assistant>()
                .HasOne(a => a.ApplicationUser)
                .WithOne()
                .HasForeignKey<Assistant>(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Year -> Group
            builder.Entity<Group>()
                .HasOne(g => g.Year)
                .WithMany(y => y.Groups)
                .HasForeignKey(g => g.YearId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student -> Enrollment
            builder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Group -> Enrollment
            builder.Entity<Enrollment>()
                .HasOne(e => e.Group)
                .WithMany(g => g.Enrollments)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Assistant <-> Year
            builder.Entity<AssistantYear>()
                .HasKey(ay => new
                {
                    ay.AssistantId,
                    ay.YearId
                });

            builder.Entity<AssistantYear>()
                .HasOne(ay => ay.Assistant)
                .WithMany(a => a.AssistantYears)
                .HasForeignKey(ay => ay.AssistantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AssistantYear>()
                .HasOne(ay => ay.Year)
                .WithMany(y => y.AssistantYears)
                .HasForeignKey(ay => ay.YearId)
                .OnDelete(DeleteBehavior.Cascade);
            // Group -> GroupSchedule
            builder.Entity<GroupSchedule>()
                .HasOne(gs => gs.Group)
                .WithMany(g => g.GroupSchedules)
                .HasForeignKey(gs => gs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);


            // Group -> ClassSession
            builder.Entity<ClassSession>()
                .HasOne(cs => cs.Group)
                .WithMany(g => g.ClassSessions)
                .HasForeignKey(cs => cs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);


            // ClassSession -> AttendanceRecord
            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.ClassSession)
                .WithMany(cs => cs.AttendanceRecords)
                .HasForeignKey(ar => ar.ClassSessionId)
                .OnDelete(DeleteBehavior.Cascade);


            // Student -> AttendanceRecord
            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.Student)
                .WithMany()
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> AttendanceRecord
            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.RecordedByUser)
                .WithMany()
                .HasForeignKey(ar => ar.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // One attendance record per student per session
            builder.Entity<AttendanceRecord>()
                .HasIndex(ar => new
                {
                    ar.ClassSessionId,
                    ar.StudentId
                })
                .IsUnique();
            // Training -> TrainingAssignment
            builder.Entity<TrainingAssignment>()
                .HasOne(ta => ta.Training)
                .WithMany(t => t.TrainingAssignments)
                .HasForeignKey(ta => ta.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);


            // Group -> TrainingAssignment
            builder.Entity<TrainingAssignment>()
                .HasOne(ta => ta.Group)
                .WithMany(g => g.TrainingAssignments)
                .HasForeignKey(ta => ta.GroupId)
                .OnDelete(DeleteBehavior.Restrict);


            // TrainingAssignment -> TrainingRecord
            builder.Entity<TrainingRecord>()
                .HasOne(tr => tr.TrainingAssignment)
                .WithMany(ta => ta.TrainingRecords)
                .HasForeignKey(tr => tr.TrainingAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);


            // Student -> TrainingRecord
            builder.Entity<TrainingRecord>()
                .HasOne(tr => tr.Student)
                .WithMany()
                .HasForeignKey(tr => tr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> Training
            builder.Entity<Training>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> TrainingRecord
            builder.Entity<TrainingRecord>()
                .HasOne(tr => tr.RecordedByUser)
                .WithMany()
                .HasForeignKey(tr => tr.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One training record per student per assignment
            builder.Entity<TrainingRecord>()
                .HasIndex(tr => new
                {
                    tr.TrainingAssignmentId,
                    tr.StudentId
                })
                .IsUnique();


            builder.Entity<TrainingRecord>()
                .Property(tr => tr.Score)
                .HasPrecision(5, 2);

            builder.Entity<TrainingRecord>()
                .Property(tr => tr.MaxScore)
                .HasPrecision(5, 2);




            // TrainingAssignment -> TrainingAttempt
            builder.Entity<TrainingAttempt>()
                .HasOne(ta => ta.TrainingAssignment)
                .WithMany(t => t.TrainingAttempts)
                .HasForeignKey(ta => ta.TrainingAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);


            // Student -> TrainingAttempt
            builder.Entity<TrainingAttempt>()
                .HasOne(ta => ta.Student)
                .WithMany()
                .HasForeignKey(ta => ta.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Exam -> ExamAssignment
            builder.Entity<ExamAssignment>()
                .HasOne(ea => ea.Exam)
                .WithMany(e => e.ExamAssignments)
                .HasForeignKey(ea => ea.ExamId)
                .OnDelete(DeleteBehavior.Cascade);


            // Group -> ExamAssignment
            builder.Entity<ExamAssignment>()
                .HasOne(ea => ea.Group)
                .WithMany(g => g.ExamAssignments)
                .HasForeignKey(ea => ea.GroupId)
                .OnDelete(DeleteBehavior.Restrict);


            // ExamAssignment -> ExamResult
            builder.Entity<ExamResult>()
                .HasOne(er => er.ExamAssignment)
                .WithMany(ea => ea.ExamResults)
                .HasForeignKey(er => er.ExamAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);


            // Student -> ExamResult
            builder.Entity<ExamResult>()
                .HasOne(er => er.Student)
                .WithMany()
                .HasForeignKey(er => er.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> Exam
            builder.Entity<Exam>()
                .HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> ExamResult
            builder.Entity<ExamResult>()
                .HasOne(er => er.RecordedByUser)
                .WithMany()
                .HasForeignKey(er => er.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExamResult>()
                .HasIndex(er => new
                {
                    er.ExamAssignmentId,
                    er.StudentId
                })
                .IsUnique();

            // Quiz -> QuizAssignment
            builder.Entity<QuizAssignment>()
                .HasOne(qa => qa.Quiz)
                .WithMany(q => q.QuizAssignments)
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);


            // Group -> QuizAssignment
            builder.Entity<QuizAssignment>()
                .HasOne(qa => qa.Group)
                .WithMany(g => g.QuizAssignments)
                .HasForeignKey(qa => qa.GroupId)
                .OnDelete(DeleteBehavior.Restrict);


            // QuizAssignment -> QuizAttempt
            builder.Entity<QuizAttempt>()
                .HasOne(qa => qa.QuizAssignment)
                .WithMany(q => q.QuizAttempts)
                .HasForeignKey(qa => qa.QuizAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);


            // Student -> QuizAttempt
            builder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Student)
                .WithMany()
                .HasForeignKey(qa => qa.StudentId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> Quiz
            builder.Entity<Quiz>()
                .HasOne(q => q.CreatedByUser)
                .WithMany()
                .HasForeignKey(q => q.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuizAttempt>()
                .HasIndex(qa => new
                {
                    qa.QuizAssignmentId,
                    qa.StudentId
                })
                .IsUnique();

            // Year -> Post
            builder.Entity<Post>()
                .HasOne(p => p.Year)
                .WithMany(y => y.Posts)
                .HasForeignKey(p => p.YearId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> Post
            builder.Entity<Post>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // Post -> PostAttachment
            builder.Entity<PostAttachment>()
                .HasOne(pa => pa.Post)
                .WithMany(p => p.Attachments)
                .HasForeignKey(pa => pa.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Group -> RegistrationRequest
            builder.Entity<RegistrationRequest>()
                .HasOne(r => r.RequestedGroup)
                .WithMany(g => g.RegistrationRequests)
                .HasForeignKey(r => r.RequestedGroupId)
                .OnDelete(DeleteBehavior.Restrict);


            // ApplicationUser -> RegistrationRequest
            builder.Entity<RegistrationRequest>()
                .HasOne(r => r.ReviewedByUser)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Conversation -> Participants
            builder.Entity<ConversationParticipant>()
                .HasKey(cp => new
                {
                    cp.ConversationId,
                    cp.UserId
                });


            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // Conversation -> Messages
            builder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);


            // ApplicationUser -> Messages
            builder.Entity<Message>()
                .HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<AttendanceSettings>()
                .Property(a => a.WarningPercentage)
                .HasPrecision(5, 2);

            builder.Entity<AttendanceSettings>()
                .Property(a => a.CriticalPercentage)
                .HasPrecision(5, 2);




            builder.Entity<Training>()
                .Property(t => t.MaximumGrade)
                .HasPrecision(5, 2);

            builder.Entity<Exam>()
                .Property(e => e.MaximumGrade)
                .HasPrecision(5, 2);

            builder.Entity<Exam>()
                .Property(e => e.PassingPercentage)
                .HasPrecision(5, 2);

            builder.Entity<ExamResult>()
                .Property(er => er.Score)
                .HasPrecision(5, 2);

            builder.Entity<ExamResult>()
                .Property(er => er.MaxScore)
                .HasPrecision(5, 2);

            builder.Entity<Quiz>()
                .Property(q => q.MaximumGrade)
                .HasPrecision(5, 2);

            builder.Entity<QuizAttempt>()
                .Property(qa => qa.Score)
                .HasPrecision(5, 2);

            builder.Entity<QuizAttempt>()
                .Property(qa => qa.MaxScore)
                .HasPrecision(5, 2);

            builder.Entity<TrainingAttempt>()
                .Property(ta => ta.Score)
                .HasPrecision(5, 2);

            builder.Entity<TrainingAttempt>()
                .Property(ta => ta.MaxScore)
                .HasPrecision(5, 2);


        }
    }
}