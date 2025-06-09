import Link from 'next/link';

export default function TeamNotFoundPage() {
  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="max-w-md w-full text-center px-4">
        <div className="mb-8">
          <div className="w-24 h-24 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <svg className="w-12 h-12 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-4">Team Not Found</h1>
          <p className="text-gray-600 mb-8">
            The team you&apos;re looking for doesn&apos;t exist or may have been removed. Please check the URL and try again.
          </p>
        </div>
        
        <div className="space-y-4">
          <Link 
            href="/" 
            className="w-full bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition-colors font-medium inline-block"
          >
            Go to TeamStride Home
          </Link>
          <p className="text-sm text-gray-500">
            Looking to create a new team? 
            <a href="/signup" className="text-blue-600 hover:text-blue-700 font-medium ml-1">
              Sign up here
            </a>
          </p>
        </div>
      </div>
    </div>
  );
} 