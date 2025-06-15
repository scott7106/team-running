'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Image from 'next/image';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faExclamationTriangle,
  faTimes,
  faEye,
  faEyeSlash,
  faPlus,
  faTrash,
  faCheck,
  faCalendarAlt,
  faUsers,
  faInfoCircle
} from '@fortawesome/free-solid-svg-icons';
import { getTeamThemeData } from '@/utils/team-theme';
import { registrationApi } from '@/utils/api';
import { SubdomainThemeDto } from '@/types/team';
import { 
  SubmitRegistrationDto, 
  AthleteRegistrationDto, 
  TeamRegistrationWindowDto
} from '@/types/registration';

interface TeamRegisterPageProps {
  teamSubdomain: string;
}

interface RegistrationFormData {
  firstName: string;
  lastName: string;
  email: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  registrationPasscode: string;
  codeOfConductAccepted: boolean;
  athletes: AthleteRegistrationDto[];
}

export default function TeamRegisterPage({ 
  teamSubdomain
}: TeamRegisterPageProps) {
  const [formData, setFormData] = useState<RegistrationFormData>({
    firstName: '',
    lastName: '',
    email: '',
    emergencyContactName: '',
    emergencyContactPhone: '',
    registrationPasscode: '',
    codeOfConductAccepted: false,
    athletes: [{
      firstName: '',
      lastName: '',
      birthdate: '',
      gradeLevel: ''
    }]
  });
  
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [error, setError] = useState('');
  const [showPasscode, setShowPasscode] = useState(false);
  const [themeData, setThemeData] = useState<SubdomainThemeDto | null>(null);
  const [registrationWindow, setRegistrationWindow] = useState<TeamRegistrationWindowDto | null>(null);
  const [loadingWindow, setLoadingWindow] = useState(true);
  const router = useRouter();

  // Load theme data and registration window
  useEffect(() => {
    const loadData = async () => {
      try {
        // Load theme data
        const themeDataResult = await getTeamThemeData(teamSubdomain);
        setThemeData(themeDataResult);
        document.title = `Join ${themeDataResult.teamName || teamSubdomain} - Registration`;

        // Load active registration window
        setLoadingWindow(true);
        const windowResult = await registrationApi.getActiveRegistrationWindow(themeDataResult.teamId);
        setRegistrationWindow(windowResult);
      } catch (error) {
        console.error('Failed to load registration data:', error);
        setError('Failed to load registration information. Please try again later.');
        // Use default theme if theme loading fails
        if (!themeData) {
          setThemeData({
            teamId: '',
            teamName: teamSubdomain.charAt(0).toUpperCase() + teamSubdomain.slice(1),
            subdomain: teamSubdomain,
            primaryColor: '#10B981',
            secondaryColor: '#F0FDF4',
            logoUrl: undefined
          });
        }
      } finally {
        setLoadingWindow(false);
      }
    };

    if (teamSubdomain) {
      loadData();
    }
  }, [teamSubdomain]); // eslint-disable-line react-hooks/exhaustive-deps

  const addAthlete = () => {
    setFormData(prev => ({
      ...prev,
      athletes: [
        ...prev.athletes,
        {
          firstName: '',
          lastName: '',
          birthdate: '',
          gradeLevel: ''
        }
      ]
    }));
  };

  const removeAthlete = (index: number) => {
    if (formData.athletes.length > 1) {
      setFormData(prev => ({
        ...prev,
        athletes: prev.athletes.filter((_, i) => i !== index)
      }));
    }
  };

  const updateAthlete = (index: number, field: keyof AthleteRegistrationDto, value: string) => {
    setFormData(prev => ({
      ...prev,
      athletes: prev.athletes.map((athlete, i) => 
        i === index ? { ...athlete, [field]: value } : athlete
      )
    }));
  };

  const updateFormField = (field: keyof Omit<RegistrationFormData, 'athletes'>, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const validateForm = (): string | null => {
    if (!formData.firstName.trim()) return 'Guardian first name is required';
    if (!formData.lastName.trim()) return 'Guardian last name is required';
    if (!formData.email.trim()) return 'Email is required';
    if (!/\S+@\S+\.\S+/.test(formData.email)) return 'Please enter a valid email address';
    if (!formData.emergencyContactName.trim()) return 'Emergency contact name is required';
    if (!formData.emergencyContactPhone.trim()) return 'Emergency contact phone is required';
    if (!formData.registrationPasscode.trim()) return 'Registration passcode is required';
    if (!formData.codeOfConductAccepted) return 'You must accept the code of conduct to continue';
    
    for (let i = 0; i < formData.athletes.length; i++) {
      const athlete = formData.athletes[i];
      if (!athlete.firstName.trim()) return `Athlete ${i + 1} first name is required`;
      if (!athlete.lastName.trim()) return `Athlete ${i + 1} last name is required`;
      if (!athlete.birthdate) return `Athlete ${i + 1} birthdate is required`;
      if (!athlete.gradeLevel.trim()) return `Athlete ${i + 1} grade level is required`;
    }
    
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    if (!themeData?.teamId) {
      setError('Team information not loaded. Please refresh the page.');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const dto: SubmitRegistrationDto = {
        email: formData.email,
        firstName: formData.firstName,
        lastName: formData.lastName,
        emergencyContactName: formData.emergencyContactName,
        emergencyContactPhone: formData.emergencyContactPhone,
        codeOfConductAccepted: formData.codeOfConductAccepted,
        registrationPasscode: formData.registrationPasscode,
        athletes: formData.athletes
      };
      
      await registrationApi.submitRegistration(themeData.teamId, dto);
      setIsSubmitted(true);
    } catch (error: unknown) {
      console.error('Registration error:', error);
      const errorMessage = error instanceof Error ? error.message : 'Registration failed. Please try again.';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  // Show loading state while data is loading
  if (!themeData || loadingWindow) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  // Apply team theming
  const themeStyles = {
    '--team-primary': themeData?.primaryColor || '#10B981',
    '--team-secondary': themeData?.secondaryColor || '#D1FAE5',
    '--team-primary-bg': themeData?.secondaryColor || '#F0FDF4',
  } as React.CSSProperties;

  // Show success message if registration is submitted
  if (isSubmitted) {
    return (
      <div 
        className="min-h-screen flex flex-col justify-center py-12 sm:px-6 lg:px-8"
        style={{ 
          ...themeStyles,
          background: `linear-gradient(to br, ${themeData?.secondaryColor || '#F0FDF4'}, white, ${themeData?.primaryColor ? `${themeData.primaryColor}10` : '#E5F7F0'})`
        }}
      >
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100 text-center">
            <div 
              className="w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4"
              style={{ backgroundColor: themeData?.primaryColor || '#10B981' }}
            >
              <FontAwesomeIcon icon={faCheck} className="text-white text-2xl" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-4">
              Registration Submitted!
            </h2>
            <p className="text-gray-600 mb-6">
              Thank you for registering with {themeData?.teamName}. Your registration has been submitted and the team will review it shortly.
            </p>
            <p className="text-sm text-gray-500 mb-6">
              You&apos;ll receive an email confirmation once your registration has been processed.
            </p>
            <button
              onClick={() => router.push('/')}
              className="w-full py-3 px-4 rounded-lg font-medium text-white transition-colors"
              style={{ backgroundColor: themeData?.primaryColor || '#10B981' }}
            >
              Return to Team Page
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Show no active registration window message
  if (!registrationWindow) {
    return (
      <div 
        className="min-h-screen flex flex-col justify-center py-12 sm:px-6 lg:px-8"
        style={{ 
          ...themeStyles,
          background: `linear-gradient(to br, ${themeData?.secondaryColor || '#F0FDF4'}, white, ${themeData?.primaryColor ? `${themeData.primaryColor}10` : '#E5F7F0'})`
        }}
      >
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="flex justify-center">
            {themeData?.logoUrl ? (
              <div className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg overflow-hidden bg-white">
                <Image 
                  src={themeData.logoUrl} 
                  alt={`${themeData.teamName || themeData.subdomain} logo`} 
                  width={48}
                  height={48}
                  className="object-contain"
                />
              </div>
            ) : (
              <div 
                className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg"
                style={{ 
                  background: `linear-gradient(to right, ${themeData?.primaryColor || '#10B981'}, ${themeData?.secondaryColor || '#059669'})` 
                }}
              >
                <span className="text-white font-bold text-2xl">
                  {themeData?.teamName?.charAt(0).toUpperCase() || themeData?.subdomain.charAt(0).toUpperCase()}
                </span>
              </div>
            )}
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {themeData?.teamName || themeData?.subdomain}
          </h2>
        </div>

        <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
          <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100 text-center">
            <FontAwesomeIcon icon={faInfoCircle} className="text-gray-400 text-4xl mb-4" />
            <h3 className="text-xl font-semibold text-gray-900 mb-4">
              Registration Not Available
            </h3>
            <p className="text-gray-600 mb-6">
              There is currently no active registration window for this team. 
              Please check back later or contact the team coaches for more information.
            </p>
            <button
              onClick={() => router.push('/')}
              className="w-full py-3 px-4 rounded-lg font-medium text-white transition-colors"
              style={{ backgroundColor: themeData?.primaryColor || '#10B981' }}
            >
              Return to Team Page
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div 
      className="min-h-screen flex flex-col justify-center py-12 sm:px-6 lg:px-8"
      style={{ 
        ...themeStyles,
        background: `linear-gradient(to br, ${themeData?.secondaryColor || '#F0FDF4'}, white, ${themeData?.primaryColor ? `${themeData.primaryColor}10` : '#E5F7F0'})`
      }}
    >
      <div className="sm:mx-auto sm:w-full sm:max-w-2xl">
        <div className="flex justify-center">
          {themeData?.logoUrl ? (
            <div className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg overflow-hidden bg-white">
              <Image 
                src={themeData.logoUrl} 
                alt={`${themeData.teamName || themeData.subdomain} logo`} 
                width={48}
                height={48}
                className="object-contain"
              />
            </div>
          ) : (
            <div 
              className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg"
              style={{ 
                background: `linear-gradient(to right, ${themeData?.primaryColor || '#10B981'}, ${themeData?.secondaryColor || '#059669'})` 
              }}
            >
              <span className="text-white font-bold text-2xl">
                {themeData?.teamName?.charAt(0).toUpperCase() || themeData?.subdomain.charAt(0).toUpperCase()}
              </span>
            </div>
          )}
        </div>
        <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Join {themeData?.teamName || themeData?.subdomain}
        </h2>
        <p className="mt-2 text-center text-sm text-gray-600">
          Register for the upcoming season
        </p>
      </div>

      {/* Registration Window Info */}
      <div className="mt-6 sm:mx-auto sm:w-full sm:max-w-2xl">
        <div 
          className="bg-white/80 backdrop-blur-sm border rounded-lg p-4 mb-6"
          style={{ borderColor: themeData?.primaryColor + '30' }}
        >
          <div className="flex items-center justify-between text-sm">
            <div className="flex items-center">
              <FontAwesomeIcon icon={faCalendarAlt} className="mr-2" style={{ color: themeData?.primaryColor }} />
              <span className="font-medium">Registration Period:</span>
              <span className="ml-2">
                {formatDate(registrationWindow.startDate)} - {formatDate(registrationWindow.endDate)}
              </span>
            </div>
            <div className="flex items-center">
              <FontAwesomeIcon icon={faUsers} className="mr-2" style={{ color: themeData?.primaryColor }} />
              <span className="font-medium">Max Athletes:</span>
              <span className="ml-2">{registrationWindow.maxRegistrations}</span>
            </div>
          </div>
        </div>
      </div>

      <div className="mt-2 sm:mx-auto sm:w-full sm:max-w-2xl">
        <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100">
          {/* Error Alert */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
              <div className="flex items-start">
                <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mt-0.5 mr-3 flex-shrink-0" />
                <div className="flex-1">
                  <p className="text-sm text-red-700">{error}</p>
                </div>
                <button
                  onClick={() => setError('')}
                  className="text-red-400 hover:text-red-600 ml-2 flex-shrink-0"
                >
                  <FontAwesomeIcon icon={faTimes} className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
          
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Guardian Information */}
            <div className="border-b border-gray-200 pb-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">Guardian Information</h3>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                    First Name *
                  </label>
                  <input
                    id="firstName"
                    name="firstName"
                    type="text"
                    required
                    value={formData.firstName}
                    onChange={(e) => updateFormField('firstName', e.target.value)}
                    className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                    style={{
                      '--tw-ring-color': themeData?.primaryColor || '#10B981',
                      'focusBorderColor': themeData?.primaryColor || '#10B981'
                    } as React.CSSProperties}
                    disabled={isLoading}
                  />
                </div>
                
                <div>
                  <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                    Last Name *
                  </label>
                  <input
                    id="lastName"
                    name="lastName"
                    type="text"
                    required
                    value={formData.lastName}
                    onChange={(e) => updateFormField('lastName', e.target.value)}
                    className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                    style={{
                      '--tw-ring-color': themeData?.primaryColor || '#10B981',
                      'focusBorderColor': themeData?.primaryColor || '#10B981'
                    } as React.CSSProperties}
                    disabled={isLoading}
                  />
                </div>
              </div>
              
              <div className="mt-4">
                <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                  Email Address *
                </label>
                <input
                  id="email"
                  name="email"
                  type="email"
                  required
                  value={formData.email}
                  onChange={(e) => updateFormField('email', e.target.value)}
                  className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                  style={{
                    '--tw-ring-color': themeData?.primaryColor || '#10B981',
                    'focusBorderColor': themeData?.primaryColor || '#10B981'
                  } as React.CSSProperties}
                  disabled={isLoading}
                />
              </div>
            </div>

            {/* Emergency Contact */}
            <div className="border-b border-gray-200 pb-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">Emergency Contact</h3>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="emergencyContactName" className="block text-sm font-medium text-gray-700 mb-1">
                    Emergency Contact Name *
                  </label>
                  <input
                    id="emergencyContactName"
                    name="emergencyContactName"
                    type="text"
                    required
                    value={formData.emergencyContactName}
                    onChange={(e) => updateFormField('emergencyContactName', e.target.value)}
                    className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                    style={{
                      '--tw-ring-color': themeData?.primaryColor || '#10B981',
                      'focusBorderColor': themeData?.primaryColor || '#10B981'
                    } as React.CSSProperties}
                    disabled={isLoading}
                  />
                </div>
                
                <div>
                  <label htmlFor="emergencyContactPhone" className="block text-sm font-medium text-gray-700 mb-1">
                    Emergency Contact Phone *
                  </label>
                  <input
                    id="emergencyContactPhone"
                    name="emergencyContactPhone"
                    type="tel"
                    required
                    value={formData.emergencyContactPhone}
                    onChange={(e) => updateFormField('emergencyContactPhone', e.target.value)}
                    className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                    style={{
                      '--tw-ring-color': themeData?.primaryColor || '#10B981',
                      'focusBorderColor': themeData?.primaryColor || '#10B981'
                    } as React.CSSProperties}
                    disabled={isLoading}
                  />
                </div>
              </div>
            </div>

            {/* Athletes */}
            <div className="border-b border-gray-200 pb-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-medium text-gray-900">Athletes</h3>
                <button
                  type="button"
                  onClick={addAthlete}
                  className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors"
                  style={{ 
                    backgroundColor: themeData?.primaryColor || '#10B981',
                    '--tw-ring-color': themeData?.primaryColor || '#10B981'
                  } as React.CSSProperties}
                  disabled={isLoading}
                >
                  <FontAwesomeIcon icon={faPlus} className="mr-1" />
                  Add Athlete
                </button>
              </div>
              
              {formData.athletes.map((athlete, index) => (
                <div key={index} className="mb-6 p-4 bg-gray-50 rounded-lg relative">
                  {formData.athletes.length > 1 && (
                    <button
                      type="button"
                      onClick={() => removeAthlete(index)}
                      className="absolute top-2 right-2 text-red-500 hover:text-red-700 transition-colors"
                      disabled={isLoading}
                    >
                      <FontAwesomeIcon icon={faTrash} className="w-4 h-4" />
                    </button>
                  )}
                  
                  <h4 className="text-sm font-medium text-gray-900 mb-3">
                    Athlete {index + 1}
                  </h4>
                  
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        First Name *
                      </label>
                      <input
                        type="text"
                        required
                        value={athlete.firstName}
                        onChange={(e) => updateAthlete(index, 'firstName', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                        style={{
                          '--tw-ring-color': themeData?.primaryColor || '#10B981',
                          'focusBorderColor': themeData?.primaryColor || '#10B981'
                        } as React.CSSProperties}
                        disabled={isLoading}
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Last Name *
                      </label>
                      <input
                        type="text"
                        required
                        value={athlete.lastName}
                        onChange={(e) => updateAthlete(index, 'lastName', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                        style={{
                          '--tw-ring-color': themeData?.primaryColor || '#10B981',
                          'focusBorderColor': themeData?.primaryColor || '#10B981'
                        } as React.CSSProperties}
                        disabled={isLoading}
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Birthdate *
                      </label>
                      <input
                        type="date"
                        required
                        value={athlete.birthdate}
                        onChange={(e) => updateAthlete(index, 'birthdate', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                        style={{
                          '--tw-ring-color': themeData?.primaryColor || '#10B981',
                          'focusBorderColor': themeData?.primaryColor || '#10B981'
                        } as React.CSSProperties}
                        disabled={isLoading}
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Grade Level *
                      </label>
                      <select
                        required
                        value={athlete.gradeLevel}
                        onChange={(e) => updateAthlete(index, 'gradeLevel', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                        style={{
                          '--tw-ring-color': themeData?.primaryColor || '#10B981',
                          'focusBorderColor': themeData?.primaryColor || '#10B981'
                        } as React.CSSProperties}
                        disabled={isLoading}
                      >
                        <option value="">Select grade level</option>
                        <option value="K">Kindergarten</option>
                        <option value="1">1st Grade</option>
                        <option value="2">2nd Grade</option>
                        <option value="3">3rd Grade</option>
                        <option value="4">4th Grade</option>
                        <option value="5">5th Grade</option>
                        <option value="6">6th Grade</option>
                        <option value="7">7th Grade</option>
                        <option value="8">8th Grade</option>
                        <option value="9">9th Grade</option>
                        <option value="10">10th Grade</option>
                        <option value="11">11th Grade</option>
                        <option value="12">12th Grade</option>
                      </select>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Registration Passcode */}
            <div className="border-b border-gray-200 pb-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">Registration Passcode</h3>
              <div className="relative">
                <input
                  id="registrationPasscode"
                  name="registrationPasscode"
                  type={showPasscode ? 'text' : 'password'}
                  required
                  value={formData.registrationPasscode}
                  onChange={(e) => updateFormField('registrationPasscode', e.target.value)}
                  placeholder="Enter registration passcode"
                  className="w-full px-3 py-3 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                  style={{
                    '--tw-ring-color': themeData?.primaryColor || '#10B981',
                    'focusBorderColor': themeData?.primaryColor || '#10B981'
                  } as React.CSSProperties}
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={() => setShowPasscode(!showPasscode)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
                  disabled={isLoading}
                >
                  <FontAwesomeIcon 
                    icon={showPasscode ? faEyeSlash : faEye} 
                    className="h-5 w-5" 
                  />
                </button>
              </div>
              <p className="mt-2 text-sm text-gray-500">
                Contact your team coach to get the registration passcode.
              </p>
            </div>

            {/* Code of Conduct */}
            <div className="pb-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">Code of Conduct</h3>
              <div className="flex items-start">
                <input
                  id="codeOfConduct"
                  name="codeOfConduct"
                  type="checkbox"
                  required
                  checked={formData.codeOfConductAccepted}
                  onChange={(e) => updateFormField('codeOfConductAccepted', e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 focus:ring-2 focus:ring-offset-2 transition-colors mt-1"
                  style={{
                    '--tw-ring-color': themeData?.primaryColor || '#10B981',
                    'accentColor': themeData?.primaryColor || '#10B981'
                  } as React.CSSProperties}
                  disabled={isLoading}
                />
                <label htmlFor="codeOfConduct" className="ml-3 text-sm text-gray-700">
                  I accept the team&apos;s code of conduct and agree to abide by all team rules and policies. *
                </label>
              </div>
            </div>

            {/* Submit Button */}
            <div>
              <button
                type="submit"
                disabled={isLoading}
                className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                style={{
                  backgroundColor: themeData?.primaryColor || '#10B981',
                  '--tw-ring-color': themeData?.primaryColor || '#10B981'
                } as React.CSSProperties}
              >
                {isLoading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Submitting Registration...
                  </>
                ) : (
                  'Submit Registration'
                )}
              </button>
            </div>
          </form>
          
          {/* Authentication Help */}
          <div className="mt-6 text-center space-y-2">
            <p className="text-sm text-gray-600">
              Already have an account?{' '}
              <a 
                href="/login" 
                className="font-medium hover:opacity-80 transition-opacity"
                style={{ color: themeData?.primaryColor || '#10B981' }}
              >
                Sign in here
              </a>
            </p>
            <p className="text-sm text-gray-600">
              Looking for a different team?{' '}
              <a 
                href="/join-team" 
                className="font-medium hover:opacity-80 transition-opacity"
                style={{ color: themeData?.primaryColor || '#10B981' }}
              >
                Find your team
              </a>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
} 