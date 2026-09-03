class RegisterRequestModel {
  final String firstName;
  final String lastName;
  final String email;
  final String password;
  final String phone;
  final String department;
  final int roleType;
  final String? subRole;

  const RegisterRequestModel({
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.password,
    required this.phone,
    required this.department,
    this.roleType = 0, // 0: Employee
    this.subRole,
  });

  Map<String, dynamic> toJson() {
    return {
      'firstName': firstName.trim(),
      'lastName': lastName.trim(),
      'email': email.trim().toLowerCase(),
      'password': password,
      'phone': phone.trim(),
      'department': department.trim(),
      'roleType': roleType,
      'subRole': subRole?.trim(),
    };
  }
}
