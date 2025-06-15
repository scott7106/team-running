interface HeroSectionProps {
  onLoginClick?: () => void;
  onPrimaryAction?: () => void;
  primaryButtonText?: string;
  showSecondaryButton?: boolean;
  onSecondaryAction?: () => void;
  secondaryButtonText?: string;
}

export default function HeroSection({
  onLoginClick,
  onPrimaryAction,
  primaryButtonText = "Start Your Free Team",
  showSecondaryButton = true,
  onSecondaryAction,
  secondaryButtonText = "Watch Demo"
}: HeroSectionProps) {
  return (
    <section className="relative overflow-hidden">
      {/* Background Elements */}
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50"></div>
      <div className="absolute top-0 left-0 w-full h-full">
        <div className="absolute top-20 left-10 w-72 h-72 bg-blue-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse"></div>
        <div className="absolute top-40 right-10 w-72 h-72 bg-purple-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse animation-delay-2000"></div>
        <div className="absolute -bottom-8 left-20 w-72 h-72 bg-indigo-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse animation-delay-4000"></div>
      </div>
      
      {/* Content */}
      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-32">
        <div className="text-center">
          {/* Main Headline */}
          <h1 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 mb-4 leading-tight animate-slide-up">
            <span className="block mb-2">
              Streamline Team Management with a{' '}
              <span className="bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 bg-clip-text text-transparent">
                Smarter Coaching Platform
              </span>
            </span>
          </h1>
          
          {/* Tagline */}
          <div className="text-lg sm:text-xl lg:text-2xl font-semibold text-gray-700 mb-8 animate-fade-in animation-delay-200">
            <span className="inline-block mr-2 sm:mr-4">Built for Educators.</span>
            <span className="inline-block">Designed for Coaches.</span>
          </div>
          
          {/* Detailed Description */}
          <p className="text-lg sm:text-xl text-gray-600 mb-10 max-w-5xl mx-auto leading-relaxed animate-fade-in animation-delay-800">
            Manage every aspect of your cross country or track program—rosters, practice schedules, 
            individualized workouts, meet calendars, race results, and team communication—in one 
            organized, easy-to-use platform. Save time, stay organized, and support student-athletes 
            with data-driven insights that complement your educational mission.
          </p>
          
          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12 animate-fade-in animation-delay-1000">
            <button 
              onClick={onPrimaryAction || onLoginClick}
              className="group bg-gradient-to-r from-blue-600 to-indigo-600 text-white px-10 py-5 rounded-xl text-lg font-bold hover:from-blue-700 hover:to-indigo-700 transition-all duration-300 transform hover:scale-105 hover:shadow-xl"
            >
              <span className="flex items-center justify-center">
                {primaryButtonText}
                <svg className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
                </svg>
              </span>
            </button>
            {showSecondaryButton && (
              <button 
                onClick={onSecondaryAction}
                className="group border-2 border-gray-300 text-gray-700 px-10 py-5 rounded-xl text-lg font-bold hover:border-blue-500 hover:text-blue-600 transition-all duration-300 transform hover:scale-105 hover:shadow-lg bg-white/80 backdrop-blur-sm"
              >
                <span className="flex items-center justify-center">
                  {secondaryButtonText === "Join Existing Team" ? (
                    <svg className="w-5 h-5 mr-2 group-hover:scale-110 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
                    </svg>
                  ) : (
                    <svg className="w-5 h-5 mr-2 group-hover:scale-110 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.828 14.828a4 4 0 01-5.656 0M9 10h1m4 0h1m-6 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  )}
                  {secondaryButtonText}
                </span>
              </button>
            )}
          </div>
          
          {/* Social Proof - Updated for Academic Context */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-8 text-sm text-gray-500 animate-fade-in animation-delay-1200">
            <div className="flex items-center">
              <div className="flex -space-x-2 mr-3">
                <div className="w-8 h-8 bg-gradient-to-r from-blue-400 to-blue-600 rounded-full border-2 border-white"></div>
                <div className="w-8 h-8 bg-gradient-to-r from-purple-400 to-purple-600 rounded-full border-2 border-white"></div>
                <div className="w-8 h-8 bg-gradient-to-r from-indigo-400 to-indigo-600 rounded-full border-2 border-white"></div>
                <div className="w-8 h-8 bg-gradient-to-r from-green-400 to-green-600 rounded-full border-2 border-white"></div>
              </div>
              <span>Trusted by 2,500+ educators & coaches</span>
            </div>
            <div className="flex items-center">
              <span className="text-yellow-400 mr-1">★★★★★</span>
              <span>4.9/5 from 200+ schools</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
} 